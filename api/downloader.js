const cliProgress = require('cli-progress');
const logging = require('../logging');
const colors = require('colors/safe');;
const download = require('download');
const colors2 = require('colors');
const crypto = require('crypto');
const fs = require('fs')

module.exports = {
    // Check file hash
    checkHash: function(file, hash) {
        const shasum = crypto.createHash('sha1');
        const data = fs.readFileSync(file, 'binary')
        shasum.update(data, 'binary');
        const value = shasum.digest('hex')
        return value === hash;
    },

    // Downloads any file
    downloadFile: async function(url, name, path, useHttps) {
        fs.writeFileSync(path + name, await download(url));
    },

    // Download minecraft
    downloadMinecraft: async function(meta, fabricMeta, path, isFabric) {
        const b1 = new cliProgress.SingleBar({
            format: 'Downloading minecraft ['.white + colors.cyan('{bar}') + '] {percentage}% - {eta_formatted} | {type} {file}'.white
        }, cliProgress.Presets.legacy);
        const dir = path + "versions/" + meta.id + "-fabric";
        const file = dir + "/minecraft.jar";
        b1.start(1, 0);
        if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
            if (fs.existsSync(file)) {
                b1.update(undefined, { file: "minecraft.jar", type: "Hash Check" });
                    if (!this.checkHash(file, meta.downloads.client.sha1)) {
                        b1.update(undefined, { file: "minecraft.jar", type: "Hash Mismatch" });
                        await this.downloadFile(meta.downloads.client.url, "minecraft.jar", dir + "/", true);
                    }
            } else {
                await this.downloadFile(meta.downloads.client.url, "minecraft.jar", dir + "/", true);
                b1.update(undefined, { file: "minecraft.jar", type: "Download" });
            }  
        
        b1.increment();
        b1.stop();
    },

    // Download Log4j config
    downloadConfig: async function(meta, path) {
        const b1 = new cliProgress.SingleBar({
            format: 'Downloading config ['.white + colors.cyan('{bar}') + '] {percentage}% - {eta_formatted} | {type} {file}'.white
        }, cliProgress.Presets.legacy);
        const configFile = meta.logging.client.file.id;
        const file = path + configFile;
        b1.start(1, 0);
        if (!fs.existsSync(path)) fs.mkdirSync(path, { recursive: true });
        if (fs.existsSync(file)) {
            b1.update(undefined, { file: configFile, type: "Hash Check" });
            if (!this.checkHash(file, meta.logging.client.file.sha1)) {
                b1.update(undefined, { file: configFile, type: "Hash Mismatch" });
                await this.downloadFile(meta.logging.client.file.url, configFile, path, true);
            }
        } else {
            b1.update(undefined, { file: configFile, type: "Download" });
            await this.downloadFile(meta.logging.client.file.url, configFile, path, true);
        }
        b1.increment();
        b1.stop();
        return configFile;
    },

    // Download assets for version
    downloadAssets: async function(meta, path) {
        const b1 = new cliProgress.SingleBar({
            format: 'Downloading assets ['.white + colors.cyan('{bar}') + '] {percentage}% - {eta_formatted} | {type} {file}'.white
        }, cliProgress.Presets.legacy);
        path += "/assets/objects/";
        b1.start(Object.keys(meta.objects).length, 0);
        for (i in meta.objects) {
            const obj = meta.objects[i];
            const hash = obj.hash;
            const ch2 = hash.slice(0, 2);
            const dir = path + "/" + ch2 + "/";
            const url = `http://resources.download.minecraft.net/${ch2}/${hash}`;
            if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
            const file = dir + hash;
            if (fs.existsSync(file)) {
                b1.update(undefined, { file: i, type: "Hash Check" });
                if (!this.checkHash(file, obj.hash)) {
                    b1.update(undefined, { file: i, type: "Hash Mismatch" });
                    await this.downloadFile(url, hash, dir);
                }
            } else {
                b1.update(undefined, { file: i, type: "Download" });
                await this.downloadFile(url, hash, dir);
            }
            b1.increment();
        }
        b1.stop();
    },

    // Download all minecrarft libs
    downloadLibs: async function(meta, fabricMeta, path, isFabric) {
        const b1 = new cliProgress.SingleBar({
            format: 'Downloading libraries ['.white + colors.cyan('{bar}') + '] {percentage}% - {eta_formatted} | {type} {file}'.white
        }, cliProgress.Presets.legacy);
        path += "libraries/";
        if (!isFabric) b1.start(Object.keys(meta.libraries).length, 0);
        else b1.start(Object.keys(meta.libraries).length + Object.keys(fabricMeta.libraries).length, 0);
        for (i in meta.libraries) {
            const lib = meta.libraries[i];
            const split = lib.downloads.artifact.path.split('/');
            const name = split[split.length - 1];
            const libPath = require('path').dirname(lib.downloads.artifact.path);
            const dir = path + libPath + "/";
            if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
            const file = dir + name;
            if (fs.existsSync(file)) {
                b1.update(undefined, { file: lib.name, type: "Hash Chech" });
                if (!this.checkHash(file, lib.downloads.artifact.sha1)) {
                    b1.update(undefined, { file: lib.name, type: "Hash Mismatch" });
                    await this.downloadFile(lib.downloads.artifact.url, name, dir, true);
                }
            } else {
                b1.update(undefined, { file: lib.name, type: "Download" });
                await this.downloadFile(lib.downloads.artifact.url, name, dir, true);
            }
            if (lib.downloads.hasOwnProperty("classifiers") && lib.downloads.classifiers.hasOwnProperty("natives-linux")) {  
                const libPath2 = path + require('path').dirname(lib.downloads.classifiers["natives-linux"].path);
                const dir2 = libPath2 + "/natives/";
                const name2 = require('path').basename(lib.downloads.classifiers["natives-linux"].path);
                const file2 = dir2 + name2;
                if (!fs.existsSync(dir2)) fs.mkdirSync(dir2, { recursive: true });
                if (fs.existsSync(file2)) {
                    b1.update(undefined, { file: lib.name, type: "Hash Check" });
                    if (!this.checkHash(file2, lib.downloads.classifiers["natives-linux"].sha1)) {
                        b1.update(undefined, { file: lib.name, type: "Hash Mismatch" });
                        await this.downloadFile(lib.downloads.classifiers["natives-linux"].url, name2, dir2, true);
                    }
                } else {
                    b1.update(undefined, { file: lib.name, type: "Download" });
                    await this.downloadFile(lib.downloads.classifiers["natives-linux"].url, name2, dir2, true);
                }
            }
            b1.increment();
        }
        if (isFabric) {
            for (i in fabricMeta.libraries) {
                const lib = fabricMeta.libraries[i];
                const split = lib.name.split(':');
                const path1 = split[0].split('.');
                const libPath = path1.join('/');
                const link = `${lib.url}${path1.join('/')}/${split[1]}/${split[2]}/${split[1]}-${split[2]}.jar`;
                const dir = path + libPath + "/" + split[1] + "/";
                if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
                b1.update(undefined, { file: lib.name, type: "Download" });
                await this.downloadFile(link, `${split[1]}-${split[2]}.jar`, dir, true);
                b1.increment();
            }
        }
        b1.stop();
    }
}