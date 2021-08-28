const logging = require('./logging');
const colors = require('colors/safe');
const readline = require('readline');
;
const loader = require("./minecraft/loader");
const runner = require("./minecraft/runner");
const fetcher = require('./minecraft/fetcher');
const fs = require('fs');

var rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout
});

function checkInternet(cb) {
    require('dns').lookup('google.com',function(err) {
        if (err && err.code == "ENOTFOUND") cb(false); 
        else cb(true);
    })
}

async function main(meta, metaFabric, path) {
    await loader.download(meta, metaFabric, path);
    rl.close();
    await runner.run(meta, metaFabric, path, metaFabric ? loader.metaDetect(metaFabric) : loader.metaDetect(meta));
}

(async () => {
    console.log('\033[2J');
    var lines = process.stdout.getWindowSize()[1];
    for(var i = 0; i < lines; i++) console.log('\r\n');
    logging.about("Blowaunch by TheAirBlow");
    logging.about("https://github.com/theairblow/blowaunch");

    if (process.platform !== "linux") {
        logging.error("Only linux is tested. Fuck Windows and OSX users!");
        process.exit();
    }

    checkInternet(async (val) => {
        if (!val) {
            logging.warn("Offline mode is not supported yet!");
            process.exit();
        }

        if (true /* false for legacy mode */) {
            rl.question(colors.magenta('[Input] ') + colors.white("Do you want to use Fabric? [y/n]: "), async (val) => {
                if (val !== "y" && val !== "n") {
                    logging.error("Wrong input! Only lowercase.");
                    process.exit();
                }
    
                rl.question(colors.magenta('[Input] ') + colors.white("Enter minecraft version: "), async (ans) => {
                    const path = require('path').join(require('os').homedir(), ".minecraft");
                    const versions = await fetcher.get('https://launchermeta.mojang.com/mc/game/version_manifest.json');
                    var ver = versions.versions.find(x => x.id === ans);
                    if (await fs.existsSync(require('path').join(path, "versions", ans)) && !ver) {
                        logging.info("Custom version! (not supported yet)");
                        process.exit();
                    }
                    if (!ver) {
                        logging.error("Unknown version!");
                        process.exit();
                    }
                    ver = await fetcher.get(ver.url);
                    if (val === "y") {
                        const loaders = await fetcher.get(`https://meta.fabricmc.net/v2/versions/loader/${ans}`);
                        const fabric = await fetcher.get(`https://meta.fabricmc.net/v2/versions/loader/${ans}/${loaders[0].loader.version}/profile/json`);
                        main(ver, fabric, path);
                    } else main(ver, undefined, path);
                });
            });
        } else {
            logging.warn("Using legacy mode!");
            const run = require('./deprecated/run');
            const fetcher = require('./deprecated/fetcher');
            const versions = require('./deprecated/versions');
            const downloader = require('./deprecated/downloader');
            const version_manifest = await versions.get();
            rl.question(colors.magenta('[Input] ') + colors.white("Do you want to use Fabric? [y/n]: "), async (val) => {
                if (val !== "y" && val !== "n") {
                    logging.error("Wrong input! Only lowercase.");
                    process.exit();
                }
                rl.question(colors.magenta('[Input] ') + colors.white("Enter minecraft version: "), async (ans) => {
                    var value;
                    try { value = version_manifest.versions.find(x => x.id === ans); if (!value) throw new Error(); } 
                    catch { logging.error("Unknown version!"); process.exit(); }
                    try {
                        rl.question(colors.magenta('[Input] ') + colors.white("Enter username: "), async (ans2) => {
                            var fabric_meta;
                            if (val) {
                                logging.info("Fetching fabric meta...");
                                const loaders = await fetcher.get(`https://meta.fabricmc.net/v2/versions/loader/${ans}`);
                                fabric_meta = await fetcher.get(`https://meta.fabricmc.net/v2/versions/loader/${ans}/${loaders[0].loader.version}/profile/json`);
                            }
                            logging.info("Fetching vanilla meta...");
                            if (!value) throw new Error();
                            const launcher_meta = await fetcher.get(value.url);
                            const assets_meta = await fetcher.get(launcher_meta.assetIndex.url);
                            const path = require('os').homedir() + "/.minecraft/blowaunch/";
                            if (!fs.existsSync(`${path}assets/indexes`)) fs.mkdirSync(`${path}assets/indexes`);
                            fs.writeFileSync(`${path}assets/indexes/${launcher_meta.assetIndex.id}.json`, JSON.stringify(assets_meta));
                            await downloader.downloadAssets(assets_meta, path);
                            await downloader.downloadLibs(launcher_meta, fabric_meta, path, val);
                            await downloader.downloadMinecraft(launcher_meta, fabric_meta, path, val);
                            const configCommand = await downloader.downloadConfig(launcher_meta, path, val);
                            logging.info(`${colors.blue("[Tasks]")} Generating run command...`);
                            const command = run.generateCommand(launcher_meta, fabric_meta, path, configCommand, ans2, val);
                            logging.info(`${colors.blue("[Tasks]")} Launching Minecraft...`);
                            exec(command, (error, stdout, stderr) => { console.log(stdout); if (stderr) console.log(stderr); });
                        });
                    } catch (e) { logging.error("Unknown error occured: " + e); process.exit(); }
                });
            });
        };
    });
})();
