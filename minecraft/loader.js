const cliProgress = require('cli-progress');
const logging = require('../logging');
const colors = require('colors/safe');
const download = require('download');
const fetcher = require('./fetcher');
const colors2 = require('colors');
const crypto = require('crypto');
const path = require('path');
const fs = require('fs');

const multibar = new cliProgress.MultiBar({
    clearOnComplete: true,
    format: '{action} {type} ['.white + '{bar}'.green + '] {percentage}% - {eta_formatted} | {file}'.white
}, cliProgress.Presets.legacy);

module.exports = { 
    sleep: function(ms) {
        return new Promise((resolve) => {
            setTimeout(resolve, ms);
        });
    },
      
    checkHash: function(file, hash) {
        const shasum = crypto.createHash('sha1');
        const data = fs.readFileSync(file, 'binary')
        shasum.update(data, 'binary');
        const value = shasum.digest('hex')
        return value === hash;
    },

    downloadFile: async function(url, name, dir) {
        fs.writeFileSync(path.join(dir, name), await download(url));
    },

    metaDetect: function(meta) {
        if (meta.hasOwnProperty('downloads') && meta.downloads.hasOwnProperty('client')) return "jar";
        else if (meta.hasOwnProperty('libraries') && meta.hasOwnProperty('mainClass')) return "classpath";
        else return "unknown";
    },

    downloadSingleAsset: async function(i, asset, dir, bar) {
        dir = path.join(dir, "assets", "objects", asset.hash.slice(0, 2));
        const hash = asset.hash;
        const ch2 = hash.slice(0, 2);
        const url = `http://resources.download.minecraft.net/${ch2}/${hash}`;
        if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
        const file = path.join(dir, asset.hash);
        if (fs.existsSync(file)) {
            bar.update(undefined, { file: path.basename(i), type: "Hash Check" });
            if (!this.checkHash(file, asset.hash)) {
                bar.update(undefined, { file: path.basename(i), type: "Hash Mismatch" });
                await this.downloadFile(url, asset.hash, dir);
            }
        } else {
            bar.update(undefined, { file: path.basename(i), type: "Download" });
            await this.downloadFile(url, asset.hash, dir);
        }
        bar.increment();
    },

    downloadAssets: async function(meta, metaType, dir) {
        if (metaType !== "jar") {
            logging.error("Download assets on non-JAR meta (how?)");
            process.exit();
        }
        if (!meta.hasOwnProperty('assetIndex')) {
            logging.error("No assets on JAR meta (how?)");
            process.exit();
        }
        dir = path.join(dir, "../", "../");
        const data = await fetcher.get(meta.assetIndex.url);
        const indexes = path.join(dir, 'assets', 'indexes');
        if (!fs.existsSync(indexes)) fs.mkdirSync(indexes, { recursive: true });
        fs.writeFileSync(path.join(indexes, meta.assetIndex.id + '.json'), JSON.stringify(data));
        const bar = multibar.create(Object.keys(data.objects).length, 0, { action: "Assets" });
        for (i in data.objects) await this.downloadSingleAsset(i, data.objects[i], dir, bar);
        bar.stop();
    },

    downloadSingleLibPlatform: async function(lib, dir, platform, bar) {
        var obj;
        var isNative;
        dir = path.join(dir, "../", "../", "libraries");
        switch (platform) {
            case "none": obj = lib.downloads.artifact; isNative = false; break;
            default: obj = lib.downloads.classifiers[platform]; isNative = true; break;
        }
        if (isNative) dir = dir = path.join(dir, path.dirname(obj.path), "natives");
        else dir = path.join(dir, path.dirname(obj.path));
        if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
        const name = path.basename(obj.path);
        const file = path.join(dir, name);
        if (fs.existsSync(file) && obj.url && obj.sha1) {
            bar.update(undefined, { file: path.basename(obj.path), type: "Hash Check" });
            if (!this.checkHash(file, obj.sha1)) {
                bar.update(undefined, { file: path.basename(obj.path), type: "Hash Mismatch" });
                await this.downloadFile(obj.url, name, dir, true);
            }
        } else if (!fs.existsSync(file)) {
            bar.update(undefined, { file: path.basename(obj.path), type: "Download" });
            await this.downloadFile(obj.url, name, dir, true);
        }
    },

    downloadSingleLib: async function(lib, dir, bar) {
        const libd = lib.downloads;
        if (!libd) {
            const split = lib.name.split(':');
            const path1 = split[0].split('.');
            const libPath = path1.join('/');
            const name = `${split[1]}-${split[2]}.jar`;
            const link = `${lib.url}${path1.join('/')}/${split[1]}/${split[2]}/${name}`;
            dir = path.join(dir, "libraries", libPath, split[1]);
            if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
            bar.update(undefined, { file: name, type: "Download Maven" });
            await this.downloadFile(link, name, dir, true);
            bar.increment();
            return;
        }
        if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
        if (libd.hasOwnProperty('classifiers')) {
            if (libd.classifiers.hasOwnProperty('natives-linux')) await this.downloadSingleLibPlatform(lib, dir, 'natives-linux', bar);
            if (libd.classifiers.hasOwnProperty('natives-windows')) await this.downloadSingleLibPlatform(lib, dir, 'natives-windows', bar);
            if (libd.classifiers.hasOwnProperty('natives-osx')) await this.downloadSingleLibPlatform(lib, dir, 'natives-osx', bar);
        }
        if (libd.hasOwnProperty('artifact')) await this.downloadSingleLibPlatform(lib, dir, 'none', bar);
        bar.increment();
    },

    downloadLibs: async function(meta, metaType, dir) {
        if (metaType === "unknown") {
            logging.error("Download libraries on unknown meta (how?)");
            process.exit();
        }
        if (!meta.hasOwnProperty('libraries')) {
            logging.error("No libraries in meta (how?)");
            process.exit();
        }
        const data = meta.libraries;
        const bar = multibar.create(Object.keys(data).length, 0, { action: "Libraries" });
        for (i in data) await this.downloadSingleLib(data[i], dir, bar);
        bar.stop();
    },

    downloadJar: async function(meta, metaType, dir) {
        dir = path.join(dir, "versions", meta.id);
        if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
        await this.downloadAssets(meta, metaType, dir);
        await this.downloadLibs(meta, metaType, dir);
        const file = path.join(dir, 'minecraft.jar');
        const bar = multibar.create(1, 0, { action: "Minecraft" });
        if (fs.existsSync(file)) {
            bar.update(undefined, { file: "minecraft.jar", type: "Hash Check" });
            if (!this.checkHash(file, meta.downloads.client.sha1)) {
                bar.update(undefined, { file: "minecraft.jar", type: "Hash Mismatch" });
                await this.downloadFile(meta.downloads.client.url, "minecraft.jar", dir + "/", true);
            }
        } else if (!fs.existsSync(file)) {
            bar.update(undefined, { file: "minecraft.jar", type: "Download" });
            await this.downloadFile(meta.downloads.client.url, "minecraft.jar", dir + "/", true);
        }
        bar.increment();
        bar.stop();
    },

    downloadClasspath: async function(meta, jarMeta, dir) {
        await this.downloadJar(jarMeta, 'jar', dir);
        await this.downloadLibs(meta, 'classpath', dir);
    },

    download: async function(meta, metaFabric, dir) {
        switch(metaFabric !== undefined ? this.metaDetect(metaFabric) : this.metaDetect(meta)) {
            case "jar":
                logging.info("Meta type: JAR (Full version)");
                await this.downloadJar(meta, 'jar', dir);
                break;
            case "classpath":
                logging.info("Meta type: Classpath (Extension for version)");
                await this.downloadClasspath(metaFabric, meta, dir);
                break;
            case "unknown":
                logging.error("Unknown meta type! Exiting...");
                process.exit();
        }
        multibar.stop();
    }
};