Vivaldi Mod Manager - BETA
====================

Vivaldi Mod Manager is an unofficial fan-made Tool that helps injecting custom user-made modifications into the [Vivaldi browser](https://vivaldi.net/en-US/).

Browser modifications differ from regular extensions that are limited by the extension APIs. They can directly influence the look and behavior of the browser, like removing the Vivaldi button, making the address bar auto-hide when not in use or desaturate all extension buttons to fit better into the currently used theme.

#### The goals of Vivaldi Mod Manager are:

*	Provide an infrastructure that allows easy customization for power users: pick only the modifications you want, no need to install huge compilations or change internal files all the time.
*	Support frequent browser updates. After the browser was updated, users should be able to re-modify it with a few simple clicks.
*	Encourage sharing of self contained modifications among users.

#### Warning
Installing third party code into the browser directly can be a security threat, as mod authors can access and intercept everything the browser UI has access to. If you are concerned about security and do not have enough knowledge to review the code of mods, you are installing, you should avoid using any browser modifications.


The Tool
------------

Vivaldi Mod Manager is a .NET based application. It currently works and was tested on Windows systems with [.NET framework 4.5](https://www.microsoft.com/net/download) (usually pre-installed).
If you are using another operating system, you can try it with the current version of [Mono](http://www.mono-project.com/download/), but there is no guarantee for it to work.

The application requires administrator privileges to run. This is is required to be able to modify files in the *Program Files* directory, where Vivaldi is installed by default.

#### Interface
The interface is very simple: A list of installed mods is presented on the left side. Each list item has a check mark, which indicates if the mod is activated. The list order also represents the loading order of the mods. The top-most mod is loaded first and the bottom-most mod is loaded last. If two mods change the same aspect, the mod loaded later overwrites the changes. The order can be changed with the two up and down buttons on the left to the mod list.

The Reload button reads the mod directory and updates the mod list, if you have installed or deleted mods after starting the Vivaldi Mod Manager. The Settings button allows you to specify the path to the Vivaldi browser. The MODIFY button applies the changes to the browser.

#### Installing Mods
When first starting the Vivaldi Mod Manager, it creates a folder `mods/mods/` in the same directory, as the executable.
Mods can be installed by copying the files into the  `mods/mods/` sub-directory.

Example structure:
```plain
root
	Vivaldi Mod Manager.exe
	mods
		mods
			my-modification-1
			    index.html
			    style.css
			my-modification-2
			    index.html
			    style.css
		loader.html
		loader.js
```
Then start Vivaldi Mod Manager or hit the Reload button, if it is already running.

#### Uninstalling Mods
Simply delete the specific folder in `mods/mods/`.

For Mod Developers
----------------------------
A mod consists of the mod folder and an `index.html` in it.
The mod is listed in the tool using the same name as the folder.
Optionally you can include a readme file, which will be displayed inside the tool when the mod is selected.

From the `index.html` you can include your `.css` and `.js`files, just as with a regular web page.

```html
<link rel="stylesheet" href="style.css">
<script src="script.js"></script>
```

Behind the Scenes
--------------------------

The work of the tool can also be done manually without too much effort. The entry point for mods is a modified `browser.html` in
`Vivaldi\Application\1.3.544.25\resources\vivaldi\` of the current Vivaldi installation (further referred as the *resources* directory). 

An additional script reference is added below the reference to bundle.js so that it is loaded after original scripts and stylesheets are loaded.
```html
<script src="bundle.js"></script>
<script src="mods/loader.js"></script>
```

Additionally a new folder 'mods' is created in the resources directory with the following structure:

```
mods
	mods
	loader.html
	loader.js
```

The mods subfolder is used for mod installations, i.e. all mods need to be copied in this folder.

`loader.js`is then loaded from the `browser.html` and imports `loader.html`using [HTML Imports](http://www.html5rocks.com/en/tutorials/webcomponents/imports/?redirect_from_locale=de).
```javascript
(function() {
  var linkElement = document.createElement('link');
  linkElement.rel = 'import';
  linkElement.href = 'mods/loader.html';
  document.head.appendChild(linkElement);
}())
```
This detour over JavaScript is required as adding the import in the `browser.html` directly would result in loading it before the original browser resources have been loaded. 

`loader.html` serves as the loading list of all the mods.
It uses HTML imports to import each mod's own`index.html`, which in turn loads the required mod stylesheets and scripts.

It looks like this:
```html
<link rel="import" href="mods/remove-white-favicon-background/index.html">
<link rel="import" href="mods/remove-vivaldi-button/index.html">
<!--link rel="import" href="mods/auto-hide-address-bar/index.html"-->
<link rel="import" href="mods/address-bar-in-title/index.html">
<link rel="import" href="mods/less-prominent-extension-buttons/index.html">
<link rel="import" href="mods/grayscale-extension-buttons/index.html">
<link rel="import" href="mods/borderless-tab-container/index.html">
<!--link rel="import" href="mods/matrix-rain-speeddial/index.html"-->
<link rel="import" href="mods/extension-button-hider/index.html">
```
Inactive mods are commented out (Vivaldi Mod Manager has then still their position in the load order when the mods are reactivated).

Vivaldi Mod Manager mainly manages the `loader.html` and copies the mod files from its own directory to the browser directory.