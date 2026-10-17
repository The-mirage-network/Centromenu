namespace Centromenu;

// VoteBanSystem intentionally has no Harmony overrides. In particular, host-side
// AddVote must be allowed to run so votes received from other clients are counted.
// Host actions initiated by the menu use KickPlayer explicitly instead.
