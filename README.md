# EmmcHaccGen

Supposed to be a replacement of ChoiDujour

Name may not be final

# How to use
Go to the [Releases](https://github.com/suchmememanyskill/EmmcHaccGen/releases) tab to grab yourself the latest release.
```
EmmcHaccGen:
  Generates required files to boot a switch. Generates BIS (boot01, bcpkg2) and the 120 system save

Usage:
  EmmcHaccGen [options]

Options:
  --keys <keys>    Path to your prod.keys file. Required argument
  --fw <fw>        Path to your firmware folder. Required argument
  --noexfat        non-Exfat generation option. Default is false
  --version        Display version information
```

# Credits

- Denn/Dennthecafebabe for being awesome in general and being the first in implementing imkv gen in [Vaporware](https://github.com/dennthecafebabe/vaporware) and [Pyhac](https://github.com/dennthecafebabe/pyhac), which this projects imkvdb gen is based on
- Thealexbarney for making libhac, which is used in this project.

# Table of working firmwares

| Firmware version | Result |
|:----------------:|:------:|
| 1.0.0 -> 6.1.0   | Untested, will likely not work |
| 6.2.0            | Does not work |
| 7.0.0 -> 8.1.0   | Untested, files generated suggests it might work |
| 9.0.0 -> 9.1.0   | Works |
