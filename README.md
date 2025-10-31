# Vintage Story Unofficial Bugfixes

A collection of bugfix patches that are not yet in a released
version of the game. Don't expect this mod to play nice with
other mods - it may or may not work and I don't verify it and
will not be adding any special compatibility. Both content and
code mods might become incompatible if they try to touch the
same parts of the code that I do.

Please note that the below may not be relevant if it's a stable
patch number difference, such as 1.21.4 vs 1.21.5, instead of a version
difference such as 1.20.x vs 1.21.x. It's just there as a catchall
reminder that this mod is probably more likely than most to break on
an update or prevent new functionality from appearing without
intervention from myself to update it.

This mod is not planned to ever support anything other than
the most recent version available, including unstable releases.
Development time going towards old versions would simply be wasted
and there are too many factors in cross-compatility. While this
mod is public in hopes of providing early patches for annoying bugs,
this does mean that work done during unstable releases will result
in new patches being unavailable for stable release players. Apologies
for the inconvenience.

## Current Patches

* VSSurvivalMod PR [#155](https://github.com/anegostudios/vssurvivalmod/pull/155);
  Issue [#1714](https://github.com/anegostudios/VintageStory-Issues/issues/1714):
  Fix reed plants dropping when broken without a knife when already harvested. Reed
  plants are supposed to never drop their root when broken without a knife. This fixes
  that and maintains breaking the fully-grown plant without a knife dropping the reed.

* VSEssentialsMod PR [#30](https://github.com/anegostudios/vsessentialsmod/pull/30):
  Applies the effects of game time to detoxification. You can now sleep off your drinks.
  By the way, did you know that a full litre of distilled alcohol provides 1.5 intoxication
  even though the maximum is 1.1? Hold ctrl while collecting your liquid to pull out 1/10th
  of your container's capacity. (See the next patch too!)

* VSSurvivalMod PR [#154](https://github.com/anegostudios/vssurvivalmod/pull/154):
  In the base game, ANY amount of alcohol will apply the intoxication of a full litre.
  This patch adds the missing line of code to scale intoxication by quantity, so 0.1L
  will add 1/10th of the intoxication as 1L as expected.

* Issue [#7353](https://github.com/anegostudios/VintageStory-Issues/issues/7353):
  There is currently absolutely no way to repair bear hide armor in the base game.
  This patch allows a large pelt, huge pelt, or any bear pelt with or without the head
  to be used to fully repair both the durability and clothing condition for any piece of
  hide armor. I chose to allow a large pelt because repair recipes are coded to require
  half as many ingredients as it takes to create the armor. I think this is reasonably
  balanced because some bears only provide large pelts, but I'm not being too particular
  about forcing the use of huge/bear hides for certain species because it's easier and I'm
  erring towards making the repair cheaper rather than more expensive because bear hide
  armor is rather weak in the first place.

* Issue [#7352](https://github.com/anegostudios/VintageStory-Issues/issues/7352):
  The base game's soldering iron recipe is weird. You can use a chisel with 1 durability
  to produce a soldering iron with 56 durability and the extra durability varies. Because
  there isn't a single clear fix for this, I decided to simply copy the durability from
  the chisel to the soldering iron. This means a 400/600 durability chisel creates a
  400/500 durability soldering iron. It's still a bit exploitable because a 500/600
  chisel creates a 500/500 soldering iron, but I don't want to change the base durability of
  either of them and we can just pretend the soldering iron has 600 durability.

* Issue [#7464](https://github.com/anegostudios/VintageStory-Issues/issues/7464):
  Fix golden takins having a 20% chance to break reed basket trap instead of the intended 40%.
  The trapping data for goats mistakenly uses the wrong prefix for takin (goat-takin-* instead of goat-takingold-*), which causes takin to use the default instead of their own value.

## Licensing

This work is distributed under the GNU GPL-3.0 license, with the
following exception: Anego Studios has permission to use any and
all code here with or without attribution and distribute it under
their own licensing terms.
