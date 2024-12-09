`latest`
- ShadowBan False by default
- banned.json renamed to banned.json
- banned loaduntracked removed reason
- Removed early escape for already banned when banning
- Unban while SQL down check and recover

`1.1.0`
- Noted JSON Rising in README
- Local can ban extended
- Output changes

`1.0.12`
- Fixed a bug with .banned checkid

`1.0.10`
- Switched to use UTC
- Standardized Local and SQL Ban's DateTime Serialization
- Properly display Permanent when appropriate
- Added periods to outputs!
- Don't bake reasons to SQL entries
- MinValue multi-zone check for safety
- .banned loaduntracked bug fixes
- .unban commands apply to SQL again
- .list format fixes
- .banned sync output change
- SQL Table name changes

`1.0.9`
- Changes to how bans are triggered and cleared

`1.0.1`
- fixed bug in server ban command

`1.0.0`
- initial release