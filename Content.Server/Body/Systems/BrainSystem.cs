using Content.Server.Body.Components;
using Content.Server.Ghost.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Pointing;
using Robust.Shared.GameObjects.Components.Localization; // imp; for Grammar

namespace Content.Server.Body.Systems
{
    public sealed class BrainSystem : EntitySystem
    {
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly GrammarSystem _grammar = default!; // imp

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
            SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>((uid, _, args) => HandleMind(uid, args.OldBody));
            SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
        }

        private void HandleMind(EntityUid newEntity, EntityUid oldEntity)
        {
            if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
                return;

            EnsureComp<MindContainerComponent>(newEntity);
            EnsureComp<MindContainerComponent>(oldEntity);

            var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
            if (HasComp<BodyComponent>(newEntity))
                ghostOnMove.MustBeDead = true;

            //IMP EDIT: brain remembers its old identity
            if (TryComp<GrammarComponent>(oldEntity, out var formerSelf) && !HasComp<GrammarComponent>(newEntity))
            { //we only need to set what the brain's pronouns are once (upon leaving the body)
                var newGrammar = EnsureComp<GrammarComponent>(newEntity);
                _grammar.SetProperNoun((newEntity, newGrammar), formerSelf.ProperNoun);
                _grammar.SetGender((newEntity, newGrammar), formerSelf.Gender);
            }
            //END IMP EDIT

            if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
                return;

            _mindSystem.TransferTo(mindId, newEntity, mind: mind);
        }

        private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
        {
            args.Cancel();
        }
    }
}
