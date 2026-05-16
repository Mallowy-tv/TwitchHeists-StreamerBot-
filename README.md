# TwitchHeists

## ✨ What it is

TwitchHeists is a Streamer.bot add-on for watchtime, points, heists, raffles, and leaderboards backed by SQLite.

## 📦 What’s included

- watchtime refresh and chat presence tracking
- points commands and a points leaderboard
- timed heists with joins, reminders, and adaptive resolutions
- multi-winner and single-winner raffles
- a root `TwitchHeists.txt` import file for Streamer.bot

## 🚀 Install

1. Build the solution in Release mode.
2. Copy the bridge output from `src\TwitchHeists.StreamerBot.Bridge\bin\Release\net48\` into your Streamer.bot extensions folder.
3. Import the root `TwitchHeists.txt` file into Streamer.bot.

## 🧭 Notes

- The root `TwitchHeists.txt` file contains the Streamer.bot import code.
- Default configuration and setup details live in the deployed bridge folder.
