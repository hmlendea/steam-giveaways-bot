#!/bin/bash
HOME_DIR=$HOME
SCRIPT_DIR=$HOME_DIR/.sgb
SCRIPT_EXE=$SCRIPT_DIR/SteamGiveawaysBot

cd $SCRIPT_DIR

while true; do
    killall chromedriver
    killall chrome
    killall SteamGiveawaysBot

    $SCRIPT_EXE
done
