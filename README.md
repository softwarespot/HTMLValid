# HTMLValid

An unofficial W3C validator for HTML/CSS files, that takes the pain out of checking multiple files at once. It currently uses the old version of the [API](https://validator.w3.org/docs/api.html), though will be updated in the near future to use the up to date version of the [API](https://validator.w3.org/docs/api.html).

## How to use

Using HTMLValid is a simple as 1-2-3 and therefore does not require a large README as one would expect. Simply drag and drop a directory containing the HTML/CSS file(s) or a single file (HTML/CSS only) and watch it whiz by as it validates at your convenience. If the directory happens to contain more than two files, it will impose a waiting period of 1 second between each check. This is due to a request by W3C, that this restriction be enforced.

## Commandline

If you're like me and enjoy using the commandline, then HTMLValid has got you covered. Pass the directory or single file (HTML/CSS only) to the executable, along with the optional parameters. See below for a complete list of supported commandline parameters with HTMLValid.

```shell
    HTMLValid.exe "C:\dev\mysite\" -optional-parameters
```

Pass the following parameters to the application:

### `directory or file`

This is mandatory that either a directory or single file be passed to the application. Failing to do so will result in the application terminating.

### `-allfiles` [optional]

As default only those files which are invalid will be displayed in the console output. By passing `-allfiles`, it will display every HTML and CSS file that is either valid or invalid.
