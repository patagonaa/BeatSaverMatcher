const path = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const package = require('./package.json');

module.exports = {
    entry: {
        app: "./src/index.ts",
        vendor: Object.keys(package.dependencies)
    },
    output: {
        path: path.resolve(__dirname, "wwwroot"),
        filename: "[name].js",
        publicPath: "/"
    },
    resolve: {
        extensions: [".js", ".ts"]
    },
    module: {
        rules: [
            {
                test: /\.ts$/,
                use: "ts-loader"
            },
            {
                test: /\.css$/,
                use: ['style-loader', 'css-loader']
            }
        ]
    },
    plugins: [
        new CleanWebpackPlugin(),
        new HtmlWebpackPlugin({
            template: "./src/index.html",
            filename: "index.html",
            chunks: ['vendor', 'app'],
            minify: {
                collapseWhitespace: true,
                keepClosingSlash: true,
                removeComments: false,
                removeRedundantAttributes: true,
                removeScriptTypeAttributes: true,
                removeStyleLinkTypeAttributes: true,
                useShortDoctype: true
            }
        }),
        new CopyWebpackPlugin({
            patterns: [
                { context: './src', from: 'icon*.png' }
            ]
        })
    ]
};