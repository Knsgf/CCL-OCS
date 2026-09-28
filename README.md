### Description
Thi framework allows Custom Car Loader (CCL) vehicles to collect power from overhead contact system (OCS) added by [1500 V DC Catenary](https://github.com/Knsgf/Derail_Valley_electric_concept/releases/) mod. Like any framework, this mod does nothing by itself.

**NB!** Like the catenary mod, electric vehicles are **not compatible** with multiplayer.

### User installation
1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) (UMM),
2. Install [Custom Car Loader](https://www.nexusmods.com/derailvalley/mods/324) (CCL) and its prerequisites,
3. Drag and drop [CatenaryForCCL.zip](https://github.com/Knsgf/CCL-OCS/releases/download/v1.0.0/CatenaryForCCL.zip) into UMM,
4. Any electric vehicle mod can now be added to UMM.

Note that there is no hard requirement to install [1500 V DC Catenary](https://github.com/Knsgf/Derail_Valley_electric_concept/releases/). This is by design, as it allows electric vehicles with a secondary onboard power source to function in sessions without OCS.

### Mod authoring installation
1. Download and install Unity editor 2019.4.4x,
2. Create new project and set it up for CCL as outlined on [CCL wiki](https://github.com/derail-valley-modding/custom-car-loader/wiki),
3. Install [CatenaryAddon.unitypackage](https://github.com/Knsgf/CCL-OCS/releases/download/v1.0.0/CatenaryAddon.unitypackage) on top of CCL to add electric vehicle components. These are found in the "Catenary Addon" category,
4. Refer to the [wiki](https://github.com/Knsgf/CCL-OCS/wiki) for detailed description of addon components,
5. When exporting a vehicle, make sure to add **CatenaryForCCL** to **Additional Dependencies** list.

An example Unity project containing a minimally functional railcar with 2 pantgraphs can be downloaded [here](https://github.com/Knsgf/CCL-OCS/releases/download/v1.0.0/example-railcar.zip). The relevant components are found in "pantograph", "pantograph2", "busBar" and "electricityMeter" inside the \[sim\] section and \[Pantograph\]/\[Pantograph2\] which contain animation.
