## Bugs

## Tech Debt
- [ ] [MED] `src\TwitchHeists.StreamerBot.Bridge\Services\BridgeRuntimeFactory.cs` — runtime ignores `ConnectionStrings.TwitchHeists` from appsettings and always builds the SQLite path from `<install>\data\twitch-heists.db` <!-- found: 2026-05-20 -->

## QA / Verification Needed
- [ ] [MED] `TwitchHeists.txt` — re-import `!raffle`/`!sraffle` into Streamer.bot and confirm `!raffle 2000` and `!sraffle 2000` honor the command amount in a live action <!-- found: 2026-05-20 -->

## Ideas / Future
