const UglifyJSPlugin = require('uglifyjs-webpack-plugin');
const path = require('path');


module.exports = {
    entry: {
        Polyfills: "./AppScripts/Polyfills/polyfills.ts"
    },
    output: {
        filename: "[name].js",
        path: path.resolve(__dirname, './dist/MyApp/Scripts')
    },
    devtool: 'source-map',
    resolve: {
        // Add '.ts' and '.tsx' as a resolvable extension.
        extensions: [".webpack.js", ".web.js", ".ts", ".tsx", ".js"]
    },
    module: {

        rules: [
            {
                test: /\.tsx?$/,
                loader: "ts-loader"
            }
        ]
    },
    plugins: [
        new UglifyJSPlugin({
            sourceMap: true
        })
    ],
    externals: {
        "../../../node_modules/moment/moment": "moment",
        "../moment": "moment",
        "moment": "moment",
        "jquery": "jQuery"
    }
};

