## Original Source

These third party assets came from Tesseract Unity:
https://github.com/Neelarghya/tesseract-unity

Imported from git commit: `867cff4`

## Original License

The original code is licensed under Apache License 2.0. The full text of the license is available in the LICENSE file.

## Local Modifications

The version of Tesseract Unity in this directory has been modified by Google LLC. The key changes include:

* Outputs JSON as a response to include wordbox metadata
* Less verbose debug output by default
* Does not redraw texture by defualt
* Alternate source for unziputil (no longer nuget)
* Modernize deprecated code for TarArchive and WWW
