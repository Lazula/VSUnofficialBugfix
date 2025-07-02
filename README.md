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

* Fixed in 1.21.0-pre.2:
  VSSurvivalMod PR [#115](https://github.com/anegostudios/vssurvivalmod/pull/115);
  Issues [#6010](https://github.com/anegostudios/VintageStory-Issues/issues/6010)
  and [#4462](https://github.com/anegostudios/VintageStory-Issues/issues/4462):
  Fix troughs only using creature diets and not trough
  suitability when displaying what can eat the contents, e.g.
  chickens cannot eat from large troughs. The display also uses the below patch
  for fixing listed creature diets, so the ones that we have to change will still
  show up properly like pigs being able to eat fruit mash.

* Fixed in 1.21.0-pre.2 (no related issues or pull requests):
  Fix creature diet food tags (e.g. fruitmash) always being empty. Restores some broken
  functionality for troughs, basket traps, beehive looting, berry bush looting, and
  crop eating. Some are already partially functional due to using food categories instead
  of food tags. **This patch will not work on modded creatures due to patching
  limitations.** The technical reason is that the weighted tags are protected instead of
  public, which causes deserialization to fail. We can't hook into the method
  IsSuitableFor that provides the diet matching method due being part of an interface,
  and hooking into creature diet construction also doesn't work because we have
  no entity data to work with. Thus the patch is implemented as just a hook on every
  vanilla implementor. Modders are free to copy the code for their own implementors.

## Licensing

This work is distributed under the GNU GPL-3.0 license, with the
following exception: Anego Studios has permission to use any and
all code here with or without attribution and distribute it under
their own licensing terms.
