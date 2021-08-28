const fetch = require('node-fetch');

module.exports = {
    get: async function(url) {
        const data = await fetch(url);
        return data.json();
    }
}