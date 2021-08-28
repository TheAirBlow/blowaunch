const StringBuilder = require("string-builder");
const cfg = require("../config.json");
const { v4: uuidv4 } = require('uuid');

module.exports = {
    // Generate classpath
    generateClasspath: function(meta, fabricMeta, path, config, isFabric) {
        const sb = new StringBuilder();
        const path2 = path;
        path += "libraries/"
        sb.append("");
        for (i in meta.libraries) {
            const lib = meta.libraries[i];
            const libpath = path + lib.downloads.artifact.path;
            sb.append(libpath + ":");
            if (lib.downloads.hasOwnProperty("classifiers") && lib.downloads.classifiers.hasOwnProperty("natives-linux")) {
                const libpath2 = path + require('path').dirname(lib.downloads.classifiers["natives-linux"].path) + "/natives/" 
                + require('path').basename(lib.downloads.classifiers["natives-linux"].path);
                sb.append(libpath2 + ":");
            }
        }
        if (isFabric) {
            for (i in fabricMeta.libraries) {
                const lib = fabricMeta.libraries[i];
                const split = lib.name.split(':');
                const path1 = split[0].split('.');
                const libPath = path1.join('/');
                sb.append(path + libPath + "/" + split[1] + "/" + `${split[1]}-${split[2]}.jar:`);
            }
        }
        sb.append(`${config}:${path2}versions/${meta.id}${isFabric ? "-fabric" : ""}/minecraft.jar`);
        return sb.toString();
    },

    // Generate command
    generateCommand: function(meta, fabricMeta, path, config, userName, isFabric) {
        const ram = cfg.ram;
        const env = cfg.env !== "" ? cfg.env + " " : "";
        const jvm = cfg.jvm !== "" ? cfg.jvm + " " : "";
        const game = cfg.game !== "" ? cfg.game + " " : "";
        const sb = new StringBuilder();
        sb.append(`cd ${path}; ${env}java -Xms${ram} -Xmx${ram} -Dlog4j.configuration=${config} ${jvm}`);
        for (i in meta.arguments.jvm) {
            const obj = meta.arguments.jvm[i];
            if (obj.hasOwnProperty("rules")) continue;
            const data = obj.replace('-Djava.library.path=', '').replace('${natives_directory}', '').replace('${launcher_name}', 'Blowaunch')
            .replace('${launcher_version}', '1.0.0').replace('${classpath}', this.generateClasspath(meta, fabricMeta, path, config, isFabric));
            sb.append(data + " ");
        }
        sb.append(`${isFabric ? fabricMeta.mainClass : meta.mainClass} ${game} ${isFabric ? "-DFabricMcEmu=net.minecraft.client.main.Main " : ""}`);
        for (i in meta.arguments.game) {
            const obj = meta.arguments.game[i];
            if (obj.hasOwnProperty("rules")) continue;
            const data = obj.replace('${auth_player_name}', userName).replace('${version_name}', meta.id).replace('${game_directory}', path)
            .replace('${assets_root}', path + "assets/").replace('${assets_index_name}', meta.assets).replace('${auth_uuid}', uuidv4())
            .replace('${auth_access_token}', 'FuckYouBitch').replace('${user_type}', 'mojang').replace('${version_type}', 'Minecraft');
            sb.append(data + " ");
        }
        return sb.toString();
    }
}