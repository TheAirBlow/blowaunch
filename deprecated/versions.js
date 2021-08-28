const fetch = require('node-fetch');

module.exports = {
    get: async function() {
        const data = await fetch("https://launchermeta.mojang.com/mc/game/version_manifest.json");
        return data.json();
    }
}