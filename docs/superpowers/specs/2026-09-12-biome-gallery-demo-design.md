# Biome Gallery Demo — Design Specification

## Purpose

Create an independent, explorable visual showcase that evaluates the large sprite library already present in the project. The demo should make the project feel broader and more cohesive without changing the current campaign rooms or committing to a full game redesign.

The scene is a visual prototype, not a new campaign level. Its primary success criterion is that the available character art, environmental composition, animation and VFX can be judged together in motion from a three-quarter presentation.

## Scene and scope

- New scene: `Assets/Scenes/Prototypes/BiomeGallery_Demo.unity`.
- One continuous map with a central plaza and three connected biomes.
- Approximately three to five minutes of exploratory traversal.
- Existing Room_01–Room_04 scenes and campaign flow remain unchanged.
- The scene is not added to the final campaign build until its visual direction is reviewed.
- No external or newly generated art is required; the prototype uses assets already in the repository.

## Spatial structure

The central plaza acts as a readable landmark and connects three organic paths:

1. **Enchanted forest** — vegetation, warm fire effects, ranger imagery and ambient character activity.
2. **Arcane ruins** — masonry, guards, energy barriers, crystals and cold magical effects.
3. **Alchemy marsh** — irregular terrain, poison gas, a cauldron or ritual focus and a mage sentry.

Each biome has a clear main route plus one short secondary branch. Paths, prop density, lighting and effects blend gradually at biome boundaries; there are no loading transitions or room-like doors. A portal at the far end of each branch returns the player to the central plaza.

## Visual direction

The layout borrows the spatial readability of *Don't Starve*: a fixed oblique view, organic paths, strong vertical silhouettes and foreground objects that overlap the player. It does not imitate that game's hand-drawn rendering; the repository's pixel-art identity remains intact.

- Use an orthographic camera with a restrained oblique presentation. Avoid a strong physical tilt that visibly flattens or skews the existing sprites.
- Create the three-quarter impression through composition, object height, sorting and occlusion.
- Use tall foreground props to add depth. Props that obscure the player should become partially transparent while obstructing the view.
- Avoid rectangular room silhouettes and rigid tile-grid presentation where possible.
- Keep scale, filtering and palette treatment consistent enough that the map reads as one exhibit despite differences between source packs.

## Asset roles

### `Character_base`

Use as the primary source for the player and any character that must move freely in four directions. Its directional idle, walk, run and equipment sets best support the map camera.

### Assassin/Mage/Rogue pack

Use selectively for lateral compositions: stationary sentries, statues that awaken, posed encounters or ambient vignettes. These sprites should not be presented as unrestricted four-direction patrols because most actions are side-facing.

### Forest Ranger pack

Use as large-format illustrative content rather than an in-world actor. Suitable placements include signboards, banners, portraits, carved totems or biome title displays. This prevents its high-resolution rendering from clashing directly with the smaller pixel characters.

### Explosion/VFX pack

Use its families as persistent or proximity-triggered exhibits:

- fire and sparks in the forest;
- vertical energy as an arcane barrier;
- ice orb, puddle or burst around a crystal;
- green gas and poison effects in the marsh;
- optional portal or ritual accents in the central plaza.

Only runtime-ready raster frames required by the chosen exhibits should be referenced. Source-format files are not redistributed as standalone art.

## Exploration and interactions

The current player controller and HUD should be reused when compatible. The player can freely traverse the full scene, but the prototype does not update campaign progression.

Points of interest activate through proximity:

- **Forest:** ranger display, campfire and ambient figures.
- **Ruins:** guard vignette, energy barrier and ice crystal.
- **Marsh:** poison cloud, alchemy focus and mage sentry.

Hazards are demonstrative. They animate, emit particles or briefly react to the player, but do not permanently block exploration and do not require puzzle completion. Small, unobtrusive labels may identify notable assets or prospective mechanics without turning the scene into a conventional menu.

## Technical organization

The scene hierarchy should separate the plaza and each biome into independent root groups. This allows a biome to be tuned, replaced or removed without affecting the others.

Small reusable components may be introduced for:

- proximity-based VFX activation;
- looping environmental animation;
- transparency of foreground occluders;
- return portals to the plaza.

Components should expose configuration in the Inspector and avoid dependencies on campaign progression. Existing systems should be reused when they already provide equivalent behavior.

Scene changes must remain targeted. Generated scene diffs and asset references should be reviewed to ensure that unrelated serialized settings are not changed.

## Validation

Visual review requires:

- one establishing screenshot showing the overall map composition;
- one reviewed screenshot for each biome;
- at least one gameplay capture demonstrating foreground overlap and three-quarter readability;
- a short manual traversal of every main route and secondary branch.

Functional verification requires:

- Unity scene validation with no missing references;
- successful EditMode suite;
- successful PlayMode suite;
- console review with no new actionable errors or exceptions.

The prototype is successful when all three biomes are distinguishable yet connected, the player remains readable while moving through foreground layers, the selected animation packs look coherent in their assigned roles, and every exhibit can be visited without becoming trapped.

## Explicit non-goals

- Replacing or joining the existing campaign levels.
- Building complete combat, stealth or puzzle systems for every displayed effect.
- Converting side-facing sprite packs into full directional animation sets.
- Adding the demo to the shipping scene list before human visual review.
- Pruning unused art packs during this implementation.
- Modifying known local-noise project settings, font assets, solution files or recovery assets.
