var colors = require('colors/safe');

module.exports = {
    // Info
    info: function(msg) {
        console.log(colors.cyan('[Info] ') + colors.white(msg));
    },

    // Warning
    warn: function(msg) {
        console.log(colors.yellow('[Warn] ') + colors.white(msg));
    },

    // Error
    error: function(msg) {
        console.log(colors.red('[Error] ') + colors.white(msg));
    },

    // About
    about: function(msg) {
        console.log(colors.green('[About] ') + colors.white(msg));
    },
}