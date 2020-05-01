# EmmcHaccGen

Supposed to be a replacement of ChoiDujour

Name may not be final

# How to use
Go to the [Releases](https://github.com/suchmememanyskill/EmmcHaccGen/releases) tab to grab yourself the latest release.
```
EmmcHaccGen:
  Generates boot files for the Nintendo Switch. Generates Boot01, bcpkg2 and the 120 system save.

Usage:
  EmmcHaccGen [options]

Options:
  --keys <keys>       Path to your keyset file
  --fw <fw>           Path to your firmware folder
  --no-exfat          noExfat switch. Add this if you don't want exfat support. Disabled by default
  --verbose           Enable verbose output. Disabled by default
  --show-nca-index    Show info about nca's, like it's titleid and type. Will not generate a firmware folder with this option enabled
  --version           Show version information
  -?, -h, --help      Show help and usage information
```

# Credits

- Denn/Dennthecafebabe for being awesome in general and being the first in implementing imkv gen in [Vaporware](https://github.com/dennthecafebabe/vaporware) and [Pyhac](https://github.com/dennthecafebabe/pyhac), which this projects imkvdb gen is based on
- Thealexbarney for making libhac, which is used in this project.

# Table of working firmwares

| Firmware version | Result |
|:----------------:|:------:|
| 1.0.0            | Works (Only no-exfat) 
| 2.0.0 -> 2.2.0   | Untested
| 2.3.0            | Works
| 3.0.0 -> 3.0.2   | Untested 
| 4.0.0 -> 4.1.0   | Works 
| 5.0.0 -> 6.1.0   | Untested 
| 6.2.0            | Does not work 
| 7.0.0 -> 8.1.0   | Untested 
| 9.0.0 -> 10.0.2  | Works 
