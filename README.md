# Vintage Story Unofficial Bugfixes

A collection of bugfix patches that are not yet in a released
version of the game. Don't expect this mod to play nice with
other mods - it may or may not work and I don't verify it and
will not be adding any special compatibility. Both content and
code mods might become incompatible if they try to touch the
same parts of the code that I do.

This mod is not planned to ever support anything other than
the most recent version available, including unstable releases.
Development time going towards old versions would simply be wasted
and there are too many factors in cross-compatility. While this
mod is public in hopes of providing early patches for annoying bugs,
this does mean that work done during unstable releases will result
in new patches being unavailable for stable release players. Apologies
for the inconvenience.

## Current Patches

* VSSurvivalMod PR [#115](https://github.com/anegostudios/vssurvivalmod/pull/115);
  Issues [#6010](https://github.com/anegostudios/VintageStory-Issues/issues/6010)
  and [#4462](https://github.com/anegostudios/VintageStory-Issues/issues/4462):
  Fix troughs only using creature diets and not trough
  suitability when displaying what can eat the contents, e.g.
  chickens cannot eat from large troughs.

* Issue [#5168](https://github.com/anegostudios/VintageStory-Issues/issues/5168):
  Fix being unable to catch female hares and foxes in basket traps that are
  either unable to mate or have already eaten enough to mate. Applies to all
  eating, alongside any modded entities with the "eatAnyway" property.

## Licensing

This work is distributed under the GNU GPL-3.0 license, with the
following exception: Anego Studios has permission to use any and
all code here with or without attribution and distribute it under
their own licensing terms.
