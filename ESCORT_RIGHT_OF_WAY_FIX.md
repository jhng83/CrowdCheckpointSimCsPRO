# Escort Right-of-Way Fix

This revision strengthens crowd yielding when an operator escorts a caught Yellow pedestrian back to the operator post.

## Behaviour

- Pursuit behaviour remains local and proportional.
- During escort, the Purple operator and Yellow pedestrian are treated as a two-person priority formation.
- Non-target pedestrians detect the escort corridor earlier and across a wider lateral envelope.
- Pedestrians in front of the formation choose the clearer side and sidestep around it.
- Head-on pedestrians slow using braking opposite their own velocity rather than being pushed opposite the operator's direction.
- If a pedestrian is directly blocking the escort and there is temporarily no safe sidestep, that pedestrian brakes/stops briefly until lateral space opens.
- Physical overlap correction preserves the caught Yellow pedestrian's escort formation and displaces the ordinary crowd agent instead.

The funnel queue model is unchanged. This right-of-way behaviour only affects released pedestrians in the main room.
