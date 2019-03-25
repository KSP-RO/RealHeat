RealHeat
by NathanKell and ferram4 (maintained by rsparkyc)
License: CC-BY-SA

This is a simple mod to correct some temperature-related things in KSP's thermal model. Later it will replace things wholesale, building on the work of goozeman and SRFirefox in addition to those above.

Right now it will:
* Calculate a shock temperature based on atmospheric composition and velocity.
* Calculate gamma based on atmospheric composition and velocity.
* Recalculate background radiation temperature (and the density-based interpolation factor used there and in other things) based on the above.

Installation:
Extract the RealHeat folder to GameData.

Changelog:

v4.9
* Recompile for KSP 1.6.1

v4.8
* Recompile for KSP 1.5.1

v4.7
* Recompile for KSP 1.4.5

v4.6
* Recompile for KSP 1.4.3

v4.5
* Recompile for KSP 1.3.1

v4.4
* Recompile for KSP 1.2.2

v4.3
* Recompile for KSP 1.1.3

v4.2
* Lower convective coefficient at low density and velocity (we were overestimating convection then).

v4.1
* Recompile for KSP 1.1.2

v4
* Update for KSP 1.1.
* Use KSP 1.1 feature of changing convective coefficient rather than shock temp when varying convection behind attached and detached shocks. All these are stated in the cfg for tuning.

v3
* Fix an issue with too-high background radiation temperature. This prevents blowups for low-temperature parts, but it may understate radiative heating during lunar-plus reentries. Pending 1.1 for a workaround KSP-side.

v2
* Recompiled for KSP 1.0.5.
* Removed AeroFX stuff, stock does it itself now.

v1.1
* Supports changing aeroFX now. Defaults to making it intense down low, so you can set the normal aeroFX settings to much lower scaling to not have flames on ascent. Supports aeroFXdensityExponent1 (default = 2.0) and aeroFXdensityMult1 default = 90), and the final density passed to aeroFX will be (density^aeroFXdensityExponent1 * aeroFXdensityMult1 + density^PhysicsGlobals.aeroFXDensityExponent) instead of just density^PhysicsGlobals.aeroFXDensityExponent.


v1.0
* Initial release version of RealHeat for KSP 1.0.4.
