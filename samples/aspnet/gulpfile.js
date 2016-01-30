'use strict';

var gulp = require('gulp');
var rename = require('gulp-rename');
var sass = require('gulp-sass');
var minifyCSS = require('gulp-minify-css');

gulp.task('all-js-css', ['copy-precompiled', 'compile-css']);

gulp.task('copy-precompiled', function () {

   var source = 'bower_components/';
   var dest = 'css/';

   gulp.src('bootstrap/dist/css/*.*', { cwd: source })
      .pipe(gulp.dest(dest));

   gulp.src('bootstrap/dist/fonts/*.*', { cwd: source })
      .pipe(gulp.dest('fonts'));

   dest = 'js/';

   gulp.src([
      'jquery/dist/jquery?(.min).{js,map}'
      , 'jquery-validation/dist/jquery.validate?(.min).{js,map}'
      , 'jquery-validation-unobtrusive/jquery.validate.unobtrusive?(.min).{js,map}'
      , 'bootstrap/dist/js/bootstrap?(.min).{js,map}'
   ], { cwd: source })
      .pipe(gulp.dest(dest));
});

gulp.task('compile-css', function () {

   var dest = 'css/';

   gulp.src('_scss/*.scss')
      .pipe(sass())
      .pipe(gulp.dest(dest))
      .pipe(minifyCSS())
      .pipe(rename({ extname: '.min.css' }))
      .pipe(gulp.dest(dest));
});
