'use strict';

var gulp = require('gulp');
var rename = require('gulp-rename');
var less = require('gulp-less');
var minifyCSS = require('gulp-minify-css');

gulp.task('all-js-css', ['copy-css', 'less-and-minify', 'copy-js']);

gulp.task('copy-css', function () {

   var source = 'bower_components/';
   var dest = 'css/';

   // copy pre-compiled files

   gulp.src('bootstrap/dist/css/*.*', { cwd: source })
      .pipe(gulp.dest(dest));

   gulp.src('bootstrap/dist/fonts/*.*', { cwd: source })
      .pipe(gulp.dest('fonts'));
});

gulp.task('less-and-minify', function () {

   var dest = 'css/';

   // less

   gulp.src('less/*.less')
      .pipe(less())
      .pipe(gulp.dest(dest))
      .pipe(minifyCSS())
      .pipe(rename({ extname: '.min.css' }))
      .pipe(gulp.dest(dest));
});

gulp.task('copy-js', function () {

   var source = 'bower_components/';
   var dest = 'js/';

   // copy pre-compiled files

   gulp.src([
      'jquery/dist/jquery?(.min).{js,map}'
      , 'jquery-validation/dist/jquery.validate?(.min).{js,map}'
      , 'jquery-validation-unobtrusive/jquery.validate.unobtrusive?(.min).{js,map}'
      , 'bootstrap/dist/js/bootstrap?(.min).{js,map}'
   ], { cwd: source })
      .pipe(gulp.dest(dest));
});
