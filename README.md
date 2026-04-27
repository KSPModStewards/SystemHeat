# System Heat

A mod for Kerbal Space Program, intended to provide a better experience for heat management, particularly geared towards resource extraction, high energy engines, and nuclear reactors.

* [Features](#features)
* [Dependencies](#dependencies)
* [Installation](#installation)
* [Contributing](#contributing)

## Features

In a nutshell, this mod has as goals.
* Create a coherent vision for the system-level heat mechanics in KSP
* Design a fairly simple but deep system built around designing ships to manage heat
* Provide better tools and visualizations to help players with those designs

### Gameplay Concept

Besides building a system that is stable and extensible without the show-stopping bugs of Core Heat, I want to represent a couple of design challenges that come up in spacecraft design. Spacecraft usually have to deal with a number of different thermal systems for different purposes, due to the varying heat generation requirements of various things on them. Humans make heat, electricity makes heat, nuclear engines make heat... and they tend to do so at different rates and in different ways. You usually can't just put 50 Thermal Control System (large) parts on the ship and you're all happy. 

The SystemHeat concept will basically take this and run with it. Allocating your heat producing parts correctly between your radiators will be important, as well as making sure you don't do stuff like pipe your reactor exhaust into your life support system. Fun times!

### Heat Loops

In this mod, all parts that interact with the system are part of an abstract Heat Loop. This represents the flow of coolant through a set of systems that produce heat and consume heat. Producers might include reactors, life support processors, ISRUs, drills and that kind of thing. Radiators would be consumers (effectively the only one at this point).

Each part that produces heat does so with an Output Temperature and an Output Flux. The Output Temperature this is the temperature of the insides of the part (say, a nuclear reactor core) - any coolant coming out of the part will be at that temperature. The Output Flux is the rate heat is produced by the part. Parts have different temperatures and fluxes depending on their nature - a nuclear reactor has a pretty high Output Temperature and a high Output Flux, a drill might have a lower Output Flux and a lower Output Temperature.

Similarly, parts that consume heat have special properties. They specifically consume heat with an Input Flux - a bigger, better radiator is likely able to handle a higher input flux. 

When parts are connected into a Heat Loop , the members of the loop contribute their Input/Output Flux and Output Temperature into it. If all the fluxes going into the loop are positive, it heats up and increases its Loop Temperature. If the flux is negative, it cools down. If the Loop Temperature gets too high, systems in the loop will be negatively affected, in proportion to how different the Loop Temperature is to their Output Temperature.

Your job, as the spacecraft engineer, is to construct Heat Loops such that they are well-balanced and basically don't heat up and melt things. There can be several different Heat Loops on a vessel with different parts assigned to them (though a part can currently only belong to one loop at a time).


## Dependencies

### Required
These components are required for the mod to function:
* [ModuleManager (4.2.3)](https://github.com/sarbian/ModuleManager)
* [Community Resource Pack (1.4.2)](https://github.com/BobPalmer/CommunityResourcePack)


## Installation

Installation with [CKAN](https://github.com/KSP-CKAN/CKAN/wiki) is recommended.

To install, place the GameData folder inside your Kerbal Space Program folder. If asked to overwrite files, please do so.

NOTE: Do NOT rename or move folders within the GameData folder - this mod uses absolute paths to assets and will break if this happens.

## Optional Patches

Some extra patches are bundled that you can use to tweak your installation. To install them, drop the correct folder from the **Extras** folder into your KSP GameData Folder.

Please note that these extras may break your ships in flight as they will make existing parts run on the SystemHeat system

* **SystemHeatHarvesters**: Converts harvesters to use SystemHeat
* **SystemHeatConverters**: Converts converters to use SystemHeat
* **SystemHeatFissionReactors**: Converts nuclear reactors to use SystemHeat
* **SystemHeatFissionEngines**: When installed, nuclear engines will have integrated reactors
* **SystemHeatCryoTanks**: When installed, tanks configured by CryoTanks will not use power and instead require cooling


## Contributing

SystemHeat uses [KSPBuildTools](https://kspbuildtools.readthedocs.io/en/stable/). Please read its documentation on how to specify your KSP install location in order to compile SystemHeat.

Pull requests are welcome. Make sure each pull request is limited to a single topic, bug, or feature and does not include unnecessary edits to project files etc.  Please include an update to the CHANGELOG.md file describing the change (and tag it with your own name if you would like).

