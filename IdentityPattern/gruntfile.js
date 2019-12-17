module.exports = function (grunt) {

    // Project configuration.
    grunt.initConfig({
        pkg: grunt.file.readJSON('package.json'),

        copy: {
            mko: {
                files: [
                    { expand: true, dest: 'dist/mko-scripts/', src: ['**.js', '**.js.map'], cwd: 'node_modules/mko-scripts/lib/' },
                    { expand: true, dest: 'dist/mko-stylesheet/', src: ['**'], cwd: 'node_modules/mko-stylesheet/dist/' },
                    { expand: true, dest: 'dist/myApp/fonts/titillium-web', src: ['**'], cwd: 'node_modules/titillium-web/dist/' },
                    { expand: true, dest: 'dist/mko-resources/', src: ['**'], cwd: 'node_modules/mko-resources/dist/' },
                    { expand: true, dest: 'dist/bootstrap/', src: ['**'], cwd: 'node_modules/mko-customized-bootstrap/dist/' }
                ]
            },
            packages: {
                files: [
                    { expand: true, dest: 'dist/jquery/', src: ['**'], cwd: 'node_modules/jquery/dist/' },
                    { expand: true, dest: 'dist/jquery-migrate/', src: ['**'], cwd: 'node_modules/jquery-migrate/dist/' },

                    { expand: true, dest: 'dist/moment/', src: ['moment.js', 'locale/pl.js'], cwd: 'node_modules/moment/' },
                    { expand: true, dest: 'dist/moment/', src: ['moment.min.js'], cwd: 'node_modules/moment/min/' },

                    { expand: true, dest: 'dist/eonasdan-bootstrap-datetimepicker/', src: ['**'], cwd: 'node_modules/eonasdan-bootstrap-datetimepicker/build/' },
                    { expand: true, dest: 'dist/eonasdan-bootstrap-datetimepicker/js', src: ['*.js'], cwd: 'node_modules/eonasdan-bootstrap-datetimepicker/src/js' },

                    { expand: true, dest: 'dist/domurl/', src: ['*.js'], cwd: 'node_modules/domurl/' },

                    { expand: false, dest: 'dist/knockout/knockout.min.js', src: ['node_modules/knockout/build/output/knockout-latest.js'] },
                    { expand: false, dest: 'dist/knockout/knockout.js', src: ['node_modules/knockout/build/output/knockout-latest.debug.js'] },

                    { expand: true, dest: 'dist/font-awesome/', src: ['**'], cwd: 'node_modules/@fortawesome/fontawesome-free' },

                    { expand: true, dest: 'dist/jquery-validation/', src: ['jquery.validate.js', 'jquery.validate.min.js'], cwd: 'node_modules/jquery-validation/dist/' },

                    { expand: true, dest: 'dist/jquery-validation-unobtrusive/', src: ['jquery.validate.unobtrusive.js', 'jquery.validate.unobtrusive.min.js'], cwd: 'node_modules/jquery-validation-unobtrusive/dist/' },

                    { expand: true, dest: 'dist/sprintf-js/', src: ['sprintf.min.js', 'sprintf.min.js.map'], cwd: 'node_modules/sprintf-js/dist/' },
                    { expand: true, dest: 'dist/sprintf-js/', src: ['sprintf.js'], cwd: 'node_modules/sprintf-js/src/' }

                ]
            }
        },

        concat: {
            // scripts that doesn't contain source map
            jsbundle: {
                options: {
                    separator: ';\n',
                    sourceMap: false
                },
                src: [
                    // All non-minified versions, processed later with uglify, in the specified order
                    "dist/jquery/jquery.js",
                    "dist/jquery-migrate/jquery-migrate.js",
                    "dist/bootstrap/js/bootstrap.js",
                    "dist/jquery-validation/jquery.validate.js",
                    "dist/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js",
                    "dist/domurl/url.js",
                    "dist/sprintf-js/sprintf.js",
                    "dist/moment/moment.js",
                    "dist/moment/locale/pl.js",
                    "dist/eonasdan-bootstrap-datetimepicker/js/bootstrap-datetimepicker.js"
                ],
                dest: 'dist/MyApp/Scripts/bundle.js'
            },

            // scripts containing the source map
            jsbundle_2: {
                options: {
                    separator: ';\n',
                    sourceMap: true
                },
                src: [
                    // All minified versions (no uglify later), in the specified order
                    "dist/mko-scripts/mkoDefaultScripts.js"
                ],
                dest: 'dist/MyApp/Scripts/bundle_2.js'
            },


            cssbundle: {
                options: {
                    sourceMap: true
                },
                // css files with references to fonts are excluded from the bundle due to rebasing problems
                src: [
                    "dist/eonasdan-bootstrap-datetimepicker/css/bootstrap-datetimepicker.min.css",
                    "dist/mko-stylesheet/css/mkoStylesheet.min.css",
                    "dist/mko-stylesheet/css/simpleLayout.min.css",
                    "dist/default/css/app.min.css"
                ],
                dest: 'dist/MyApp/css/bundle.css',
                nonull: true
            }

        },

        uglify: {
            options: {
                sourceMap: true,
                mangle: false
            },

            jsbundle: {
                options: {
                    mangle: false,
                    sourceMap: true
                },
                dest: 'dist/MyApp/Scripts/bundle.min.js',
                src: 'dist/MyApp/Scripts/bundle.js'
            }

        },

        exec: {
            webpack: '.\\node_modules\\.bin\\webpack --mode production',
            css: "npm run css"
        }
    });

    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-uglify');
    grunt.loadNpmTasks('grunt-exec');

    grunt.registerTask('default', ['exec', 'copy', 'concat', 'uglify']);

};
