using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

public sealed class DasherIncidentTests
{
    [Fact]
    public void OnlyOneFunnelPedestrianCanBeMarkedAtATime()
    {
        CrowdSimulation simulation = new(new SimulationConfig());
        Pedestrian first = simulation.Pedestrians.First(p => p.Phase == PedestrianPhase.Funnel);
        Pedestrian second = simulation.Pedestrians.First(p => p.Phase == PedestrianPhase.Funnel && p.Id != first.Id);

        Assert.True(simulation.TrySelectDasher(first.Id));
        Assert.False(simulation.CanSelectDasher);
        Assert.Equal(DasherState.MarkedInFunnel, first.DasherState);
        Assert.False(simulation.TrySelectDasher(second.Id));
    }

    [Fact]
    public void ResetClearsDasherSelectionLock()
    {
        CrowdSimulation simulation = new(new SimulationConfig());
        Pedestrian first = simulation.Pedestrians.First(p => p.Phase == PedestrianPhase.Funnel);

        Assert.True(simulation.TrySelectDasher(first.Id));
        simulation.Reset();

        Assert.True(simulation.CanSelectDasher);
        Assert.Null(simulation.DasherPedestrianId);
    }

    [Fact]
    public void DasherRuleTimingMatchesRequestedGuardrails()
    {
        Assert.Equal(2, DasherRules.RequiredCatchers);
        Assert.Equal(0.50f, DasherRules.CatchDistance, 3);
        Assert.Equal(1.00f, DasherRules.GroupStopSeconds, 3);
        SimulationConfig config = new();
        Assert.Equal(10.00f, config.DasherClearanceSeconds, 3);
        Assert.Equal(2.50f, DasherRules.DashSpeed, 3);
        Assert.Equal(3.50f, DasherRules.OperatorPursuitSpeed, 3);
        Assert.Equal(1.00f, DasherRules.OperatorPursuitSpeed - DasherRules.DashSpeed, 3);
        Assert.Equal(0.00f, DasherRules.SingleCatchHoldSpeed, 3);
    }
    [Fact]
    public void ConfiguredDasherClearanceControlsRemovalFromHoldingPoint()
    {
        SimulationConfig config = new()
        {
            DasherClearanceSeconds = 12.0f,
        };
        CrowdSimulation simulation = new(config);
        Pedestrian dasher = simulation.Pedestrians.First(p => p.Phase == PedestrianPhase.Funnel);

        Assert.True(simulation.TrySelectDasher(dasher.Id));
        dasher.Phase = PedestrianPhase.Room;
        dasher.DetectionStatus = DetectionStatus.DasherIncident;
        dasher.DasherState = DasherState.HeldAtHoldingPoint;
        dasher.CooperativeStop = true;
        dasher.Position = DasherRules.HoldingPoint(config);
        dasher.Velocity = System.Numerics.Vector2.Zero;

        for (int i = 0; i < 11 * 60; i++)
        {
            simulation.Advance(1.0f / 60.0f);
        }

        Assert.Equal(dasher.Id, simulation.DasherPedestrianId);
        Assert.Contains(simulation.Pedestrians, p => p.Id == dasher.Id);

        for (int i = 0; i < 70; i++)
        {
            simulation.Advance(1.0f / 60.0f);
        }

        Assert.Null(simulation.DasherPedestrianId);
        Assert.DoesNotContain(simulation.Pedestrians, p => p.Id == dasher.Id);
    }

    [Fact]
    public void DasherHoldingPointMatchesApprovedLowerLeftArea()
    {
        SimulationConfig config = new()
        {
            HallLength = 25.0f,
            RoomHeight = 15.0f,
        };

        System.Numerics.Vector2 holdingPoint = DasherRules.HoldingPoint(config);

        Assert.Equal(DasherRules.HoldingPointX, holdingPoint.X, 3);
        Assert.Equal(DasherRules.HoldingPointY, holdingPoint.Y, 3);
    }

    [Fact]
    public void MarkedFrontPassengerActivatesAtCheckpointWithoutChangingNormalOutcomeQuota()
    {
        CrowdSimulation simulation = new(new SimulationConfig());
        Pedestrian front = simulation.Pedestrians
            .Where(p => p.Phase == PedestrianPhase.Funnel)
            .OrderBy(p => p.Position.X)
            .First();

        Assert.True(simulation.TrySelectDasher(front.Id));

        for (int i = 0; i < 180; i++)
        {
            simulation.Advance(1f / 60f);
            if (front.DasherState == DasherState.Dashing)
            {
                break;
            }
        }

        Assert.Equal(PedestrianPhase.Room, front.Phase);
        Assert.Equal(DasherState.Dashing, front.DasherState);
        Assert.Equal(DetectionStatus.DasherIncident, front.DetectionStatus);
        Assert.Equal(
            simulation.Metrics.TotalCrossings,
            simulation.Metrics.TotalGreen + simulation.Metrics.TotalYellow + simulation.Metrics.TotalRed);
    }


    [Fact]
    public void FirstCatchStopsPairAndMultipleRespondersCanCompleteApprehension()
    {
        SimulationConfig config = new()
        {
            OperatorAcquisitionRadius = 5.0f,
        };
        CrowdSimulation simulation = new(config);

        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(9.0f, 5.0f)));
        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(1.0f, 4.0f)));
        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(1.0f, 6.0f)));

        Pedestrian dasher = simulation.Pedestrians
            .Where(p => p.Phase == PedestrianPhase.Funnel)
            .OrderBy(p => p.Position.X)
            .First();
        Assert.True(simulation.TrySelectDasher(dasher.Id));

        // Put the already-selected person just inside the main hall so the test can isolate the
        // apprehension state machine from queue travel time.
        dasher.Phase = PedestrianPhase.Room;
        dasher.DetectionStatus = DetectionStatus.DasherIncident;
        dasher.DasherState = DasherState.Dashing;
        dasher.Position = new System.Numerics.Vector2(9.0f, 5.0f);
        dasher.Velocity = new System.Numerics.Vector2(-DasherRules.DashSpeed, 0.0f);

        simulation.Advance(1.0f / 60.0f);

        Assert.Equal(DasherState.SingleOperatorControl, dasher.DasherState);
        Assert.Equal(1, simulation.DasherCatcherCount);
        Assert.True(dasher.CooperativeStop);
        Assert.Equal(System.Numerics.Vector2.Zero, dasher.Velocity);
        Assert.Equal(OperatorState.DasherControlling, simulation.Operators[0].State);
        Assert.Equal(System.Numerics.Vector2.Zero, simulation.Operators[0].Velocity);

        // Bring two still-free responders into acquisition/catch range together. Both may start
        // toward the same Dasher, but as soon as the second physical catch is registered the
        // apprehension team is locked to exactly two Purple operators. Any additional responder
        // must disengage and return to its own station.
        simulation.Operators[1].Position = dasher.Position;
        simulation.Operators[2].Position = dasher.Position;
        simulation.Advance(1.0f / 60.0f);

        Assert.Equal(DasherState.GroupStop, dasher.DasherState);
        Assert.Equal(2, simulation.DasherCatcherCount);
        Assert.Equal(System.Numerics.Vector2.Zero, dasher.Velocity);

        CheckpointOperator[] secured = simulation.Operators
            .Where(op => op.State == OperatorState.DasherGroupStop)
            .ToArray();
        CheckpointOperator[] released = simulation.Operators
            .Where(op => op.State == OperatorState.Returning)
            .ToArray();

        Assert.Equal(2, secured.Length);
        Assert.Single(released);
        Assert.Null(released[0].TargetPedestrianId);

        for (int i = 0; i < 70; i++)
        {
            simulation.Advance(1.0f / 60.0f);
        }

        Assert.Equal(DasherState.EscortingToHolding, dasher.DasherState);
        Assert.False(dasher.CooperativeStop);
        Assert.Equal(2, simulation.Operators.Count(op => op.State == OperatorState.DasherEscorting));
        Assert.Equal(1, simulation.Operators.Count(op => op.State == OperatorState.Returning || op.State == OperatorState.IdleAtPost));
    }


    [Fact]
    public void ExtraResponderReturningAfterTwoCatchersKeepsDasherRightOfWay()
    {
        SimulationConfig config = new()
        {
            OperatorAcquisitionRadius = 5.0f,
        };
        CrowdSimulation simulation = new(config);

        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(9.0f, 5.0f)));
        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(1.0f, 4.0f)));
        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(1.0f, 6.0f)));

        Pedestrian dasher = simulation.Pedestrians
            .Where(p => p.Phase == PedestrianPhase.Funnel)
            .OrderBy(p => p.Position.X)
            .First();
        Assert.True(simulation.TrySelectDasher(dasher.Id));

        dasher.Phase = PedestrianPhase.Room;
        dasher.DetectionStatus = DetectionStatus.DasherIncident;
        dasher.DasherState = DasherState.Dashing;
        dasher.Position = new System.Numerics.Vector2(9.0f, 5.0f);
        dasher.Velocity = new System.Numerics.Vector2(-DasherRules.DashSpeed, 0.0f);

        simulation.Advance(1.0f / 60.0f);
        simulation.Operators[1].Position = dasher.Position;
        simulation.Operators[2].Position = dasher.Position;
        simulation.Advance(1.0f / 60.0f);

        CheckpointOperator released = Assert.Single(
            simulation.Operators.Where(op => op.State == OperatorState.Returning));
        Assert.True(released.DasherReturnRightOfWay);
    }

    [Fact]
    public void VerifyingOperatorCanDropYellowForDasherAndYellowFinishesRemainingTimeAutonomously()
    {
        SimulationConfig config = new()
        {
            OperatorAcquisitionRadius = 5.0f,
            InitialManualClearanceSeconds = 10.0f,
        };
        CrowdSimulation simulation = new(config);
        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(5.0f, 5.0f)));

        CheckpointOperator op = simulation.Operators[0];
        Pedestrian yellow = simulation.Pedestrians.First(p => p.Phase == PedestrianPhase.Funnel);
        Pedestrian dasher = simulation.Pedestrians.First(
            p => p.Phase == PedestrianPhase.Funnel && p.Id != yellow.Id);

        yellow.Phase = PedestrianPhase.Room;
        yellow.DetectionStatus = DetectionStatus.CountedNotDetected;
        yellow.Position = new System.Numerics.Vector2(5.3f, 5.0f);
        yellow.Velocity = System.Numerics.Vector2.Zero;
        yellow.CooperativeStop = true;

        op.State = OperatorState.Verifying;
        op.TargetPedestrianId = yellow.Id;
        op.Position = op.HomePosition;
        op.Velocity = System.Numerics.Vector2.Zero;
        op.VerificationElapsedSeconds = 4.0f;

        Assert.True(simulation.TrySelectDasher(dasher.Id));
        dasher.Phase = PedestrianPhase.Room;
        dasher.DetectionStatus = DetectionStatus.DasherIncident;
        dasher.DasherState = DasherState.Dashing;
        dasher.Position = new System.Numerics.Vector2(8.5f, 5.0f);
        dasher.Velocity = new System.Numerics.Vector2(-DasherRules.DashSpeed, 0.0f);

        simulation.Advance(1.0f / 60.0f);

        Assert.Equal(OperatorState.DasherPursuing, op.State);
        Assert.Equal(dasher.Id, op.TargetPedestrianId);
        Assert.True(yellow.AutonomousVerificationActive);
        Assert.InRange(yellow.AutonomousVerificationRemainingSeconds, 5.95f, 6.0f);
        Assert.True(yellow.CooperativeStop);
        Assert.Equal(DetectionStatus.CountedNotDetected, yellow.DetectionStatus);

        for (int i = 0; i < 370; i++)
        {
            simulation.Advance(1.0f / 60.0f);
        }

        Assert.False(yellow.AutonomousVerificationActive);
        Assert.Equal(DetectionStatus.DetectedAndCounted, yellow.DetectionStatus);
        Assert.False(yellow.CooperativeStop);
        Assert.Equal(1, simulation.Metrics.TotalManualVerified);

        // The abandoned case completes on its own, so it does not count as a Purple-completed
        // verification for that operator's fatigue progression.
        Assert.Equal(0, op.CompletedVerificationCount);
    }

    [Theory]
    [InlineData(OperatorState.ConfirmingYellow)]
    [InlineData(OperatorState.Pursuing)]
    [InlineData(OperatorState.Stopping)]
    [InlineData(OperatorState.Escorting)]
    public void DasherPreemptsYellowHandlingBeforeVerification(OperatorState yellowState)
    {
        SimulationConfig config = new()
        {
            OperatorAcquisitionRadius = 5.0f,
        };
        CrowdSimulation simulation = new(config);
        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(5.0f, 5.0f)));

        CheckpointOperator op = simulation.Operators[0];
        Pedestrian yellow = simulation.Pedestrians.First(p => p.Phase == PedestrianPhase.Funnel);
        Pedestrian dasher = simulation.Pedestrians.First(
            p => p.Phase == PedestrianPhase.Funnel && p.Id != yellow.Id);

        yellow.Phase = PedestrianPhase.Room;
        yellow.DetectionStatus = DetectionStatus.CountedNotDetected;
        yellow.Position = new System.Numerics.Vector2(5.6f, 5.0f);
        yellow.Velocity = System.Numerics.Vector2.Zero;
        yellow.CooperativeStop = yellowState is not (OperatorState.ConfirmingYellow or OperatorState.Pursuing);

        op.State = yellowState;
        op.TargetPedestrianId = yellow.Id;
        op.TargetHasStopped = yellowState is not (OperatorState.ConfirmingYellow or OperatorState.Pursuing);
        op.Position = new System.Numerics.Vector2(5.4f, 5.0f);
        op.Velocity = System.Numerics.Vector2.Zero;

        Assert.True(simulation.TrySelectDasher(dasher.Id));
        dasher.Phase = PedestrianPhase.Room;
        dasher.DetectionStatus = DetectionStatus.DasherIncident;
        dasher.DasherState = DasherState.Dashing;
        dasher.Position = new System.Numerics.Vector2(8.0f, 5.0f);
        dasher.Velocity = new System.Numerics.Vector2(-DasherRules.DashSpeed, 0.0f);

        simulation.Advance(1.0f / 60.0f);

        Assert.Equal(OperatorState.DasherPursuing, op.State);
        Assert.Equal(dasher.Id, op.TargetPedestrianId);
        Assert.False(yellow.CooperativeStop);
        Assert.False(yellow.AutonomousVerificationActive);
        Assert.Equal(DetectionStatus.CountedNotDetected, yellow.DetectionStatus);
    }

    [Fact]
    public void ReturningOperatorAlsoReprioritizesToDasherWithinAcquisitionRadius()
    {
        SimulationConfig config = new()
        {
            OperatorAcquisitionRadius = 5.0f,
        };
        CrowdSimulation simulation = new(config);
        Assert.True(simulation.AddOperatorPost(new System.Numerics.Vector2(5.0f, 5.0f)));

        CheckpointOperator op = simulation.Operators[0];
        op.State = OperatorState.Returning;
        op.Position = new System.Numerics.Vector2(5.8f, 5.0f);
        op.Velocity = System.Numerics.Vector2.Zero;

        Pedestrian dasher = simulation.Pedestrians.First(p => p.Phase == PedestrianPhase.Funnel);
        Assert.True(simulation.TrySelectDasher(dasher.Id));
        dasher.Phase = PedestrianPhase.Room;
        dasher.DetectionStatus = DetectionStatus.DasherIncident;
        dasher.DasherState = DasherState.Dashing;
        dasher.Position = new System.Numerics.Vector2(8.0f, 5.0f);
        dasher.Velocity = new System.Numerics.Vector2(-DasherRules.DashSpeed, 0.0f);

        simulation.Advance(1.0f / 60.0f);

        Assert.Equal(OperatorState.DasherPursuing, op.State);
        Assert.Equal(dasher.Id, op.TargetPedestrianId);
    }

    [Theory]
    [InlineData(OperatorState.ConfirmingYellow)]
    [InlineData(OperatorState.Pursuing)]
    [InlineData(OperatorState.Stopping)]
    [InlineData(OperatorState.Escorting)]
    [InlineData(OperatorState.Verifying)]
    [InlineData(OperatorState.Returning)]
    [InlineData(OperatorState.DasherPursuing)]
    [InlineData(OperatorState.DasherControlling)]
    [InlineData(OperatorState.DasherGroupStop)]
    [InlineData(OperatorState.DasherEscorting)]
    public void PurpleAwayFromPostAlwaysHasRightOfWay(OperatorState state)
    {
        CheckpointOperator op = new()
        {
            Id = 1,
            Position = new System.Numerics.Vector2(6.0f, 5.0f),
            HomePosition = new System.Numerics.Vector2(5.0f, 5.0f),
            Velocity = System.Numerics.Vector2.Zero,
        };
        op.State = state;

        Assert.True(op.HasRightOfWay);

        op.Position = op.HomePosition;
        Assert.False(op.HasRightOfWay);
    }

}
