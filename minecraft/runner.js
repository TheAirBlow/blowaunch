const StringBuilder = require("string-builder");
const { v4: uuidv4 } = require('uuid');
const cfg = require("../config.json");
const logging = require('../logging');
const colors = require('colors/safe');
const readline = require('readline');
const { exec } = require("child_process")

module.exports = {
    appendPlatform: function(lib, path, platform, sb, libs) {
        const dublicate = libs.find(x => x.name.split(':')[1] == lib.name.split(':')[1]);
        if (dublicate && dublicate.name.split(':')[2] !== lib.name.split(':')[2]) {
            logging.warn(`Dublicate version: ${lib.name.split(':')[1]} (${dublicate.name.split(':')[2]}/${lib.name.split(':')[2]})`);
            return;
        }
        if (lib.downloads.hasOwnProperty("classifiers") && lib.downloads.classifiers.hasOwnProperty(platform)) {
            sb.append(require('path').join(path, require('path').dirname(lib.downloads.classifiers[platform].path), 
            'natives', require('path').basename(lib.downloads.classifiers[platform].path) + ":"));
        }
        libs.push(lib);
    },

    generateClasspath: function(meta, metaClasspath, path, metaType) {
        const sb = new StringBuilder();
        const path2 = path;
        const libs = [];
        path = require('path').join(path, 'libraries');
        sb.append("");
        for (i in meta.libraries) {
            const lib = meta.libraries[i];
            const dublicate = libs.find(x => x.name.split(':')[1] == lib.name.split(':')[1]);
            if (dublicate && dublicate.name.split(':')[2] !== lib.name.split(':')[2]) {
                logging.warn(`Dublicate version: ${lib.name.split(':')[1]} (${dublicate.name.split(':')[2]}/${lib.name.split(':')[2]})`);
                continue;
            }
            sb.append(require('path').join(path, lib.downloads.artifact.path) + ":");
            libs.push(lib);
            if (process.platform == "linux") this.appendPlatform(lib, path, 'natives-linux', sb, libs);
            if (process.platform == "windows") this.appendPlatform(lib, path, 'natives-windows', sb, libs);
            if (process.platform == "darwin") this.appendPlatform(lib, path, 'natives-osx', sb, libs);
        }
        if (metaType === "classpath") {
            for (i in metaClasspath.libraries) {
                const lib = metaClasspath.libraries[i];
                const split = lib.name.split(':');
                const path1 = split[0].split('.');
                const libPath = path1.join('/');
                sb.append(require('path').join(path, libPath, split[1], `${split[1]}-${split[2]}.jar`) + ':');
            }
        }
        sb.append(require('path').join(path2, 'versions', meta.id, 'minecraft.jar') + ':');
        return sb.toString();
    },

    run: function(meta, metaClasspath, path, metaType) {
        readline.createInterface({
            input: process.stdin,
            output: process.stdout
        }).question(colors.magenta('[Input] ') + colors.white("Enter username: "), async (userName) => { 
            const ram = cfg.ram;
            const env = cfg.env !== "" ? cfg.env + " " : "";
            const jvm = cfg.jvm !== "" ? cfg.jvm + " " : "";
            const game = cfg.game !== "" ? cfg.game + " " : "";
            const sb = new StringBuilder();
            sb.append(`cd ${path}; ${env}java -Xms${ram} -Xmx${ram} ${jvm}`);
            for (i in meta.arguments.jvm) {
                const obj = meta.arguments.jvm[i];
                if (obj.hasOwnProperty("rules")) continue;
                const data = obj.replace('-Djava.library.path=', '').replace('${natives_directory}', '').replace('${launcher_name}', 'Blowaunch')
                .replace('${launcher_version}', '1.0.0').replace('${classpath}', this.generateClasspath(meta, metaClasspath, path, metaType));
                sb.append(data + " ");
            }
            sb.append(`${metaType === "classpath" ? metaClasspath.mainClass : meta.mainClass} ${game}`);
            for (i in meta.arguments.game) {
                const obj = meta.arguments.game[i];
                if (obj.hasOwnProperty("rules")) continue;
                const data = obj.replace('${auth_player_name}', userName).replace('${version_name}', meta.id).replace('${game_directory}', path)
                .replace('${assets_root}', require("path").join(path, 'assets')).replace('${assets_index_name}', meta.assets).replace('${auth_uuid}', uuidv4())
                .replace('${auth_access_token}', 'FuckYouBitch').replace('${user_type}', 'mojang').replace('${version_type}', 'Minecraft');
                sb.append(data + " ");
            }
            
            const ex = exec(sb.toString());
            ex.stdout.on('data', function (data) {
                logging.info(data.toString());
            });

            ex.stderr.on('data', function (data) {
                logging.error(data.toString());
            });

            ex.on('exit', function (code) {
                logging.info('Exit code: ' + code.toString());
                process.exit();
            });
        });
    }
}
