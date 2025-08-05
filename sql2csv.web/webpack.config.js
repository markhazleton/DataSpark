const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = {
  entry: {
    main: './src/js/main.js',
    styles: './src/css/main.scss'
  },
  output: {
    path: path.resolve(__dirname, 'wwwroot'),
    filename: 'js/[name].bundle.js',
    clean: false
  },
  module: {
    rules: [
      {
        test: /\.scss$/,
        use: [
          MiniCssExtractPlugin.loader,
          'css-loader',
          'postcss-loader',
          'sass-loader'
        ]
      },
      {
        test: /\.css$/,
        use: [
          MiniCssExtractPlugin.loader,
          'css-loader',
          'postcss-loader'
        ]
      }
    ]
  },
  plugins: [
    new MiniCssExtractPlugin({
      filename: 'css/site.css'
    })
  ],
  resolve: {
    extensions: ['.js', '.scss', '.css']
  }
};
