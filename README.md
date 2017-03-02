# TicketBooth

## TODO

Server-side:

- Figure out what needs to be done for session expiration to be handled gracefully
 - How long do auth cookies last?
- Implement a throttling layer under ThunderTix.Session
- Make everything async
 - Async "constructors" via static methods
- `Sql.fs` is disgusting and insecure, nuke it ASAP

Android-side:

- Log-in
- Network calls

Deployment-side:

- DNS proxying
- Hardware solution (RPi?)