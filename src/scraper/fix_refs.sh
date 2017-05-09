#!/bin/bash
sed -i 's/\/css\/web_hwwkfar.css/css\/web.css/g' evaluations/*
sed -i 's/\/css\/web_defaultprint.css/css\/print.css/g' evaluations/*
sed -i 's/\/wpigifs\/wpi_logo.bmp/img\/logo.bmp/g' evaluations/*
sed -i 's/\/wtlgifs\/twggspac.gif/img\/spacer.gif/g' evaluations/*
