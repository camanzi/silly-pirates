using System.Runtime.CompilerServices;

// The test assemblies reach a handful of `internal` setters on data ScriptableObjects whose fields are
// private + [SerializeField] (TurnAgentDataSO, and the event channels of TurnOrderDataSO / TurnStateSO).
// InternalsVisibleTo is preferred over reflection: a rename breaks the build instead of rotting silently.
[assembly: InternalsVisibleTo("SillyPirates.Tests.EditMode")]
[assembly: InternalsVisibleTo("SillyPirates.Tests.PlayMode")]
