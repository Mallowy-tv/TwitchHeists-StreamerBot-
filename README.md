# TwitchHeists

## 🎮 What this is

TwitchHeists is a Twitch chat add-on that adds points, watchtime, heists, raffles, and a leaderboard.

It works with Streamer.bot and saves everything in a local SQLite database.

## ✅ What you get

- viewer watchtime tracking
- points commands
- a points leaderboard
- heists with joins and countdowns
- raffles with one or many winners

## 🚀 How to install

1. Download the release files from GitHub.
2. Put them in your Streamer.bot extensions folder like this:

```text
D:\Streamer Bot\Extensions\TwitchHeist\
  TwitchHeists.txt
  TwitchHeists.StreamerBot.Bridge.dll
  TwitchHeists.StreamerBot.dll
  TwitchHeists.Core.dll
  TwitchHeists.Data.Sqlite.dll
  appsettings.json
  heist-messages.json
```

3. Import the file [TwitchHeists.txt](./TwitchHeists.txt) into Streamer.bot.

4. Enable Live viewers in Streamer.bot: Platforms > Twitch > Present Viewers > Live Update.

![](./Assets/image.png)

## ℹ️ Notes

- `TwitchHeists.txt` is the import file for Streamer.bot.
- The rest of the setup lives in the files that come with the project.
