using System.Numerics;
using CrowdCheckpointSim.Configuration;
using CrowdCheckpointSim.Core;
using Xunit;

namespace CrowdCheckpointSim.Tests;

/// <summary>Robot assignment, shared guardrails, complete clearance, and typed setup persistence.</summary>
public sealed class RobotResponderTests
{
    [Fact]
    public void RobotPursuesImmediatelyWhilePurpleStillVisuallyConfirms()
    {
        foreach (ResponderKind kind in Enum.GetValues<ResponderKind>().Distinct())
        {
            CrowdSimulation simulation = CreateSimulation();
            Assert.True(simulation.AddOperatorPost(new Vector2(2f, 2f), kind));
            Pedestrian target = MakeUndetected(simulation, 0, new Vector2(5f, 2f));
            simulation.Advance(1f / 60f);
            CheckpointOperator responder = Assert.Single(simulation.Operators);
            Assert.Equal(target.Id, responder.TargetPedestrianId);
            Assert.Equal(kind == ResponderKind.WheeledRobot
                ? OperatorState.Pursuing : OperatorState.ConfirmingYellow, responder.State);
            if (responder.IsRobot)
            {
                Assert.Equal(0f, responder.TargetConfirmationElapsedSeconds);
                Assert.True(responder.Velocity.Length() > 0f);
            }
            else
            {
                Assert.Equal(responder.HomePosition, responder.Position);
                Assert.True(responder.TargetConfirmationElapsedSeconds > 0f);
            }
        }
    }

    [Fact]
    public void PurpleGetsFirstChoiceEvenWhenRobotWasPlacedFirstAndIsCloser()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(4f, 2f), ResponderKind.WheeledRobot));
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 2f)));
        Pedestrian target = MakeUndetected(simulation, 0, new Vector2(5f, 2f));

        simulation.Advance(1f / 60f);

        CheckpointOperator human = Assert.Single(simulation.Operators, op => !op.IsRobot);
        CheckpointOperator robot = Assert.Single(simulation.Operators, op => op.IsRobot);
        Assert.Equal(target.Id, human.TargetPedestrianId);
        Assert.Equal(OperatorState.ConfirmingYellow, human.State);
        Assert.Null(robot.TargetPedestrianId);
        Assert.Equal(OperatorState.IdleAtPost, robot.State);
    }

    [Theory]
    [InlineData(OperatorState.ConfirmingYellow)]
    [InlineData(OperatorState.Pursuing)]
    [InlineData(OperatorState.Stopping)]
    [InlineData(OperatorState.Escorting)]
    [InlineData(OperatorState.Verifying)]
    public void RobotNeverStealsPurpleLock(OperatorState humanState)
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 2f)));
        Assert.True(simulation.AddOperatorPost(new Vector2(8f, 5f), ResponderKind.WheeledRobot));
        Pedestrian target = MakeUndetected(simulation, 0, new Vector2(5f, 5f));
        CheckpointOperator human = Assert.Single(simulation.Operators, op => !op.IsRobot);
        human.State = humanState;
        human.TargetPedestrianId = target.Id;

        simulation.Advance(1f / 60f);

        CheckpointOperator robot = Assert.Single(simulation.Operators, op => op.IsRobot);
        Assert.Equal(target.Id, human.TargetPedestrianId);
        Assert.Null(robot.TargetPedestrianId);
        Assert.Equal(OperatorState.IdleAtPost, robot.State);
    }

    [Fact]
    public void RobotTakesUnreservedPassengerBesidePurpleLockedPassenger()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 2f)));
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 6f), ResponderKind.WheeledRobot));
        Pedestrian locked = MakeUndetected(simulation, 0, new Vector2(5f, 2f));
        Pedestrian free = MakeUndetected(simulation, 1, new Vector2(5f, 6f));
        CheckpointOperator human = Assert.Single(simulation.Operators, op => !op.IsRobot);
        human.State = OperatorState.ConfirmingYellow;
        human.TargetPedestrianId = locked.Id;

        simulation.Advance(1f / 60f);

        CheckpointOperator robot = Assert.Single(simulation.Operators, op => op.IsRobot);
        Assert.Equal(locked.Id, human.TargetPedestrianId);
        Assert.Equal(free.Id, robot.TargetPedestrianId);
        Assert.Equal(OperatorState.Pursuing, robot.State);
    }

    [Fact]
    public void MultipleRobotsCannotReserveSameNormalPassenger()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 2f), ResponderKind.WheeledRobot));
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 5f), ResponderKind.WheeledRobot));
        MakeUndetected(simulation, 0, new Vector2(5f, 2f));
        MakeUndetected(simulation, 1, new Vector2(5f, 5f));

        simulation.Advance(1f / 60f);

        Assert.All(simulation.Operators, op => Assert.NotNull(op.TargetPedestrianId));
        Assert.Equal(2, simulation.Operators.Select(op => op.TargetPedestrianId).Distinct().Count());
    }

    [Fact]
    public void RobotKeepsAcquisitionRadiusAndExcludesAutonomousVerification()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 1f), ResponderKind.WheeledRobot));
        Pedestrian outside = MakeUndetected(simulation, 0, new Vector2(8f, 1f));
        Pedestrian verifying = MakeUndetected(simulation, 1, new Vector2(3f, 1f));
        verifying.AutonomousVerificationActive = true;
        verifying.AutonomousVerificationRemainingSeconds = 100f;

        simulation.Advance(1f / 60f);

        Assert.Equal(OperatorState.IdleAtPost, Assert.Single(simulation.Operators).State);
        Assert.Null(simulation.Operators[0].TargetPedestrianId);
        Assert.False(outside.UndetectedCatchRecorded);
    }

    [Fact]
    public void RobotClearsAtCatchLocationThenReturnsToOriginalDockWhenNoNextTarget()
    {
        CrowdSimulation simulation = new(new SimulationConfig
        {
            TargetFunnelDensity = 0.5f,
            RobotManualClearanceSeconds = 2f,
            YellowPercent = 0f,
            InitialManualClearanceSeconds = 180f,
            ManualClearanceFatigueIncreasePercent = 100f,
        });
        Vector2 dock = new(2f, 5f);
        Assert.True(simulation.AddOperatorPost(dock, ResponderKind.WheeledRobot));
        Pedestrian target = MakeUndetected(simulation, 0, new Vector2(6.3f, 5f));
        CheckpointOperator robot = Assert.Single(simulation.Operators);
        robot.Position = new Vector2(6f, 5f); // Catch far from the docking station.
        robot.State = OperatorState.Pursuing;
        robot.TargetPedestrianId = target.Id;
        simulation.Advance(1f / 60f);
        Assert.Equal(OperatorState.Stopping, robot.State);
        Assert.Equal(1, robot.CatchCount);
        Assert.True(target.UndetectedCatchRecorded);

        HashSet<OperatorState> states = new() { robot.State };
        for (int step = 0; step < 100 && robot.State != OperatorState.Verifying; step++)
        {
            simulation.Advance(1f / 60f);
            states.Add(robot.State);
        }
        Assert.Equal(OperatorState.Verifying, robot.State);
        Assert.DoesNotContain(OperatorState.Escorting, states);
        Vector2 clearancePosition = target.Position;
        Vector2 robotPosition = robot.Position;
        Assert.True(Vector2.Distance(dock, clearancePosition) > 3f);
        Assert.Equal(2f, robot.CurrentVerificationDuration);
        for (int step = 0; step < 60; step++)
        {
            simulation.Advance(1f / 60f);
            Assert.Equal(OperatorState.Verifying, robot.State);
            Assert.Equal(clearancePosition, target.Position);
            Assert.Equal(robotPosition, robot.Position);
            Assert.True(target.CooperativeStop);
        }
        for (int step = 0; step < 100 && robot.State == OperatorState.Verifying; step++)
            simulation.Advance(1f / 60f);

        Assert.Equal(OperatorState.Returning, robot.State);
        Assert.Null(robot.TargetPedestrianId);
        Assert.Equal(DetectionStatus.DetectedAndCounted, target.DetectionStatus);
        Assert.True(Vector2.Distance(clearancePosition, target.Position) < 0.25f);
        Assert.False(target.CooperativeStop);
        Assert.Equal(1, robot.CompletedVerificationCount);
        Assert.Equal(1, simulation.Metrics.TotalManualVerified);
        Assert.Equal(2f, robot.CurrentVerificationDuration); // Fixed after each completed case.
        Assert.Equal(1f, robot.HealthFraction);

        simulation.Advance(1f / 60f); // The one post-clearance check finds no candidate.
        Assert.False(robot.CheckForNextTargetAfterClearance);
        MakeUndetected(simulation, 1, new Vector2(7f, 6f));
        for (int step = 0; step < 600 && robot.State != OperatorState.IdleAtPost; step++)
        {
            Assert.Equal(OperatorState.Returning, robot.State);
            Assert.Null(robot.TargetPedestrianId); // No new job while returning.
            simulation.Advance(1f / 60f);
        }
        Assert.Equal(OperatorState.IdleAtPost, robot.State);
        Assert.Equal(dock, robot.Position);
    }

    [Fact]
    public void CompletedRobotInterceptsNearbyPassengerFromCurrentPositionAndClearsSecondCase()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 2f), ResponderKind.WheeledRobot));
        CheckpointOperator robot = PrepareFinishingRobotClearance(simulation);
        simulation.Advance(1f / 60f);
        Assert.True(robot.CheckForNextTargetAfterClearance);
        Vector2 clearancePosition = robot.Position;
        Pedestrian next = MakeUndetected(simulation, 1, new Vector2(8f, 5f));
        Assert.True(Vector2.Distance(next.Position, robot.HomePosition) > simulation.Config.OperatorAcquisitionRadius);

        simulation.Advance(1f / 60f);

        Assert.Equal(next.Id, robot.TargetPedestrianId);
        Assert.Equal(OperatorState.Pursuing, robot.State);
        Assert.False(robot.CheckForNextTargetAfterClearance);
        Assert.True(robot.Position.X >= clearancePosition.X); // No step toward the dock.
        Assert.Equal(0f, robot.VerificationElapsedSeconds);
        Assert.Equal(1, robot.CompletedVerificationCount);
        float duration = robot.CurrentVerificationDuration;
        for (int step = 0; step < 15 * 60 && robot.CompletedVerificationCount < 2; step++)
        {
            Assert.False(robot.State == OperatorState.Returning && !robot.CheckForNextTargetAfterClearance);
            simulation.Advance(1f / 60f);
        }
        Assert.Equal(2, robot.CompletedVerificationCount);
        Assert.Equal(2, simulation.Metrics.TotalManualVerified);
        Assert.Equal(duration, robot.CurrentVerificationDuration);
        Assert.Equal(DetectionStatus.DetectedAndCounted, next.DetectionStatus);
        Assert.Equal(new Vector2(1f, 2f), robot.HomePosition);
    }

    [Theory]
    [InlineData(OperatorState.ConfirmingYellow)]
    [InlineData(OperatorState.Pursuing)]
    [InlineData(OperatorState.Stopping)]
    [InlineData(OperatorState.Escorting)]
    [InlineData(OperatorState.Verifying)]
    public void CompletedRobotNeverStealsNearbyPurpleLock(OperatorState humanState)
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 2f), ResponderKind.WheeledRobot));
        Assert.True(simulation.AddOperatorPost(new Vector2(3f, 7f)));
        CheckpointOperator robot = PrepareFinishingRobotClearance(simulation);
        simulation.Advance(1f / 60f);
        Pedestrian locked = MakeUndetected(simulation, 1, new Vector2(8f, 5f));
        CheckpointOperator human = Assert.Single(simulation.Operators, op => !op.IsRobot);
        human.State = humanState;
        human.TargetPedestrianId = locked.Id;

        simulation.Advance(1f / 60f);

        Assert.Equal(locked.Id, human.TargetPedestrianId);
        Assert.Null(robot.TargetPedestrianId);
        Assert.Equal(OperatorState.Returning, robot.State);
        Assert.False(robot.CheckForNextTargetAfterClearance);
    }

    [Fact]
    public void IdlePurpleReservesBeforeCompletedRobotEvenWhenRobotIsCloser()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 2f), ResponderKind.WheeledRobot));
        Assert.True(simulation.AddOperatorPost(new Vector2(3f, 5f)));
        CheckpointOperator robot = PrepareFinishingRobotClearance(simulation);
        simulation.Advance(1f / 60f);
        Pedestrian next = MakeUndetected(simulation, 1, new Vector2(8f, 5f));

        simulation.Advance(1f / 60f);

        CheckpointOperator human = Assert.Single(simulation.Operators, op => !op.IsRobot);
        Assert.Equal(next.Id, human.TargetPedestrianId);
        Assert.Equal(OperatorState.ConfirmingYellow, human.State);
        Assert.Null(robot.TargetPedestrianId);
        Assert.Equal(OperatorState.Returning, robot.State);
    }

    [Fact]
    public void CompletedRobotUsesRangeFromCurrentPositionAndExcludesAutonomousClearance()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 2f), ResponderKind.WheeledRobot));
        CheckpointOperator robot = PrepareFinishingRobotClearance(simulation);
        simulation.Advance(1f / 60f);
        MakeUndetected(simulation, 1, new Vector2(1f, 6f)); // Near dock, outside current acquisition range.
        Pedestrian autonomous = MakeUndetected(simulation, 2, new Vector2(8f, 5f));
        autonomous.AutonomousVerificationActive = true;
        autonomous.AutonomousVerificationRemainingSeconds = 100f;

        simulation.Advance(1f / 60f);

        Assert.Equal(OperatorState.Returning, robot.State);
        Assert.Null(robot.TargetPedestrianId);
        Assert.False(robot.CheckForNextTargetAfterClearance);
    }

    [Fact]
    public void TwoCompletedRobotsCannotReserveSameNextPassenger()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 2f), ResponderKind.WheeledRobot));
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 8f), ResponderKind.WheeledRobot));
        PrepareFinishingRobotClearance(simulation, 0, 0, new Vector2(6f, 4f));
        PrepareFinishingRobotClearance(simulation, 1, 1, new Vector2(6f, 6f));
        simulation.Advance(1f / 60f);
        Pedestrian next = MakeUndetected(simulation, 2, new Vector2(8f, 5f));

        simulation.Advance(1f / 60f);

        CheckpointOperator pursuing = Assert.Single(simulation.Operators, op => op.TargetPedestrianId == next.Id);
        Assert.Equal(OperatorState.Pursuing, pursuing.State);
        Assert.Equal(OperatorState.Returning,
            Assert.Single(simulation.Operators, op => op.Id != pursuing.Id).State);
        Assert.All(simulation.Operators, op => Assert.False(op.CheckForNextTargetAfterClearance));
    }

    [Fact]
    public void CompletedRobotIgnoresNearbyDasher()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 2f), ResponderKind.WheeledRobot));
        CheckpointOperator robot = PrepareFinishingRobotClearance(simulation);
        simulation.Advance(1f / 60f);
        MakeDasher(simulation, 1, new Vector2(8f, 5f));

        simulation.Advance(1f / 60f);

        Assert.Equal(OperatorState.Returning, robot.State);
        Assert.Null(robot.TargetPedestrianId);
        Assert.Equal(0, simulation.DasherCatcherCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Mode2CompletedRobotChainsOnlyNearbyRedAlarms(bool nearby)
    {
        CrowdSimulation simulation = new(new SimulationConfig
        {
            Mode = CheckpointMode.MultiLaneBarricade,
            FunnelWidth = 15f,
            TargetFunnelDensity = 0.5f,
        });
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 2f), ResponderKind.WheeledRobot));
        CheckpointOperator robot = PrepareFinishingRobotClearance(simulation);
        simulation.Advance(1f / 60f);
        Pedestrian next = MakeUndetected(simulation, 1,
            nearby ? new Vector2(8f, 5f) : new Vector2(1f, 1f));
        next.DetectionStatus = DetectionStatus.NotCountedNotDetected;
        next.RequiresManualClearance = true;
        MakeUndetected(simulation, 2, new Vector2(7f, 6f)); // Mode 2 Yellow remains ineligible.

        simulation.Advance(1f / 60f);

        Assert.Equal(nearby ? next.Id : (int?)null, robot.TargetPedestrianId);
        Assert.Equal(nearby ? OperatorState.Pursuing : OperatorState.Returning, robot.State);
        Assert.False(robot.CheckForNextTargetAfterClearance);
    }

    [Fact]
    public void RestartClearsPendingRobotAcquisitionAndRetainsOriginalDock()
    {
        CrowdSimulation simulation = CreateSimulation();
        Vector2 dock = new(1f, 2f);
        Assert.True(simulation.AddOperatorPost(dock, ResponderKind.WheeledRobot));
        CheckpointOperator robot = PrepareFinishingRobotClearance(simulation);
        simulation.Advance(1f / 60f);
        Assert.True(robot.CheckForNextTargetAfterClearance);

        simulation.Reset();

        robot = Assert.Single(simulation.Operators);
        Assert.False(robot.CheckForNextTargetAfterClearance);
        Assert.Equal(OperatorState.IdleAtPost, robot.State);
        Assert.Equal(dock, robot.Position);
        Assert.Equal(dock, robot.HomePosition);
    }

    /// <summary>Put a reserved passenger just before on-site clearance completion, far from the dock.</summary>
    private static CheckpointOperator PrepareFinishingRobotClearance(
        CrowdSimulation simulation, int robotIndex = 0, int passengerIndex = 0, Vector2? position = null)
    {
        CheckpointOperator robot = simulation.Operators.Where(op => op.IsRobot).ElementAt(robotIndex);
        robot.Position = position ?? new Vector2(7f, 5f);
        Pedestrian target = MakeUndetected(simulation, passengerIndex, robot.Position + new Vector2(0.4f, 0f));
        if (simulation.Config.Mode == CheckpointMode.MultiLaneBarricade)
        {
            target.DetectionStatus = DetectionStatus.NotCountedNotDetected;
            target.RequiresManualClearance = true;
        }
        target.CooperativeStop = true;
        target.UndetectedCatchRecorded = true;
        robot.State = OperatorState.Verifying;
        robot.TargetPedestrianId = target.Id;
        robot.TargetHasStopped = true;
        robot.CatchCount = 1;
        robot.OnSiteRobotPosition = robot.Position;
        robot.OnSitePassengerPosition = target.Position;
        robot.VerificationElapsedSeconds = robot.CurrentVerificationDuration - 1f / 120f;
        return robot;
    }

    [Fact]
    public void RobotAndPurpleHaveIdenticalReturnAndPedestrianYieldDynamics()
    {
        CrowdSimulation humanSimulation = CreateSimulation();
        CrowdSimulation robotSimulation = CreateSimulation();
        Pedestrian humanNeighbour = PrepareReturningResponder(humanSimulation, ResponderKind.PurpleOperator);
        Pedestrian robotNeighbour = PrepareReturningResponder(robotSimulation, ResponderKind.WheeledRobot);

        for (int step = 0; step < 10; step++)
        {
            humanSimulation.Advance(1f / 60f);
            robotSimulation.Advance(1f / 60f);
            Assert.Equal(humanSimulation.Operators[0].Position, robotSimulation.Operators[0].Position);
            Assert.Equal(humanNeighbour.Position, robotNeighbour.Position);
            Assert.Equal(humanNeighbour.Velocity, robotNeighbour.Velocity);
            Assert.True(robotSimulation.Operators[0].HasRightOfWay);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RobotsIgnoreDasherAndOnlyHumansCountTowardTwoCatchers(int humanCount)
    {
        CrowdSimulation simulation = CreateSimulation();
        float centreY = simulation.Config.HallWidth * 0.5f;
        Assert.True(simulation.AddOperatorPost(new Vector2(5f, centreY), ResponderKind.WheeledRobot));
        for (int i = 0; i < humanCount; i++)
            Assert.True(simulation.AddOperatorPost(new Vector2(3f, centreY - 1f + i * 2f)));
        Pedestrian dasher = MakeDasher(simulation, 0, new Vector2(7f, centreY));
        CheckpointOperator robot = Assert.Single(simulation.Operators, op => op.IsRobot);
        for (int step = 0; step < 6 * 60; step++)
        {
            simulation.Advance(1f / 60f);
            Assert.Equal(OperatorState.IdleAtPost, robot.State);
            Assert.Null(robot.TargetPedestrianId);
            Assert.Equal(robot.HomePosition, robot.Position);
            if (simulation.DasherCatcherCount == 2) break;
        }
        Assert.Equal(humanCount, simulation.DasherCatcherCount);
        if (humanCount == 2)
        {
            Assert.Equal(DasherState.GroupStop, simulation.CurrentDasherState);
            Assert.All(simulation.Operators.Where(op => !op.IsRobot), op =>
                Assert.Equal(OperatorState.DasherGroupStop, op.State));
        }
        else if (humanCount == 1)
            Assert.Equal(DasherState.SingleOperatorControl, dasher.DasherState);
    }

    [Theory]
    [InlineData(OperatorState.Pursuing)]
    [InlineData(OperatorState.Stopping)]
    [InlineData(OperatorState.Verifying)]
    [InlineData(OperatorState.Returning)]
    public void NearbyDasherCannotPreemptBusyRobot(OperatorState state)
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 5f), ResponderKind.WheeledRobot));
        Pedestrian target = MakeUndetected(simulation, 0, new Vector2(6f, 5f));
        CheckpointOperator robot = Assert.Single(simulation.Operators);
        robot.Position = new Vector2(5f, 5f);
        robot.State = state;
        robot.TargetPedestrianId = state == OperatorState.Returning ? null : target.Id;
        if (state == OperatorState.Verifying)
        {
            robot.OnSiteRobotPosition = robot.Position;
            robot.OnSitePassengerPosition = target.Position;
        }
        MakeDasher(simulation, 1, new Vector2(8f, 5f));
        simulation.Advance(1f / 60f);
        Assert.Equal(state, robot.State);
        Assert.Equal(state == OperatorState.Returning ? (int?)null : target.Id, robot.TargetPedestrianId);
        Assert.False(target.AutonomousVerificationActive);
        Assert.Equal(0, simulation.DasherCatcherCount);
        if (state == OperatorState.Verifying)
            Assert.True(robot.VerificationElapsedSeconds > 0f);
    }

    [Fact]
    public void Mode2RobotUsesRedAlarmAndDoesNotInterceptSecondPassYellow()
    {
        CrowdSimulation simulation = new(new SimulationConfig
        {
            Mode = CheckpointMode.MultiLaneBarricade,
            FunnelWidth = 15f,
            TargetFunnelDensity = 0.5f,
            OperatorAcquisitionRadius = 4f,
            MultiLaneManualClearanceSeconds = 180f,
            RobotManualClearanceSeconds = 7f,
        });
        Assert.True(simulation.AddOperatorPost(new Vector2(1f, 1f), ResponderKind.WheeledRobot));
        MakeUndetected(simulation, 0, new Vector2(2f, 1f)); // Mode 2 Yellow is not an intervention target.
        Pedestrian red = MakeUndetected(simulation, 1, new Vector2(8f, 8f));
        red.DetectionStatus = DetectionStatus.NotCountedNotDetected;
        red.RequiresManualClearance = true;

        simulation.Advance(1f / 60f);

        CheckpointOperator robot = Assert.Single(simulation.Operators);
        Assert.Equal(red.Id, robot.TargetPedestrianId);
        Assert.Equal(OperatorState.Pursuing, robot.State);
        Assert.Equal(7f, robot.CurrentVerificationDuration);
        robot.CompletedVerificationCount = 20;
        Assert.Equal(7f, robot.CurrentVerificationDuration);
        Assert.Equal(1f, robot.HealthFraction);
        Assert.False(robot.UseVerificationFatigue);
    }

    [Fact]
    public void Mode2OnSiteClearanceOpensSameGateAndPreservesRecordedRedOutcome()
    {
        CrowdSimulation simulation = new(new SimulationConfig
        {
            Mode = CheckpointMode.MultiLaneBarricade,
            FunnelWidth = 15f,
            TargetFunnelDensity = 0.5f,
            LaneClearanceSeconds = 3f,
            RobotManualClearanceSeconds = 1f,
        });
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 5.5f), ResponderKind.WheeledRobot));
        for (int step = 0; step < 12; step++) simulation.Advance(1f / 60f);
        CheckpointLaneGate gate = simulation.CheckpointGates[5];
        Assert.Equal(CheckpointGateState.Processing, gate.State);
        Pedestrian red = Assert.Single(simulation.Pedestrians, p => p.Id == gate.ActivePedestrianId);
        red.BarricadeOutcome = BarricadeOutcome.ManualRed;
        for (int step = 0; step < 400 && gate.State != CheckpointGateState.ManualIntervention; step++)
            simulation.Advance(1f / 60f);
        Assert.Equal(CheckpointGateState.ManualIntervention, gate.State);
        Assert.True(red.CheckpointOutcomeRecorded);
        Assert.True(red.RequiresManualClearance);
        CheckpointOperator robot = Assert.Single(simulation.Operators);
        robot.Position = new Vector2(simulation.Config.RoomWidth - simulation.Config.OperatorRadius - 0.01f, gate.CentreY);
        robot.TargetPedestrianId = red.Id;
        robot.TargetHasStopped = true;
        robot.State = OperatorState.Pursuing;
        for (int step = 0; step < 120 && robot.State != OperatorState.Verifying; step++)
            simulation.Advance(1f / 60f);
        Assert.Equal(OperatorState.Verifying, robot.State);
        Assert.Equal(PedestrianPhase.Funnel, red.Phase);
        Assert.True(red.Position.X >= simulation.Config.RoomWidth);
        for (int step = 0; step < 100 && robot.CompletedVerificationCount == 0; step++)
            simulation.Advance(1f / 60f);
        Assert.Equal(1, robot.CompletedVerificationCount);
        Assert.Equal(CheckpointGateState.Open, gate.State);
        Assert.Equal(red.Id, gate.ActivePedestrianId);
        Assert.Equal(DetectionStatus.DetectedAndCounted, red.DetectionStatus);
        Assert.False(red.RequiresManualClearance);
        Assert.Equal(BarricadeOutcome.ManualRed, red.BarricadeOutcome);
        Assert.True(red.CheckpointOutcomeRecorded);
        for (int step = 0; step < 180 && red.Phase == PedestrianPhase.Funnel; step++)
            simulation.Advance(1f / 60f);
        Assert.Equal(PedestrianPhase.Room, red.Phase);
        Assert.NotEqual(red.Id, gate.ActivePedestrianId);
        Assert.Equal(BarricadeOutcome.ManualRed, red.BarricadeOutcome);
        Assert.True(red.QueueMetricsRecorded);
    }

    [Fact]
    public void RobotIdentitySurvivesResetAndHallResizeWhileFatigueResets()
    {
        SimulationConfig config = new() { HallWidth = 20f, OperatorInterceptionSpeedLossPercent = 10f };
        CrowdSimulation simulation = new(config);
        Assert.True(simulation.AddOperatorPost(new Vector2(5f, 19f)));
        Assert.True(simulation.AddOperatorPost(new Vector2(5f, 5f), ResponderKind.WheeledRobot));
        CheckpointOperator robot = Assert.Single(simulation.Operators, op => op.IsRobot);
        robot.CatchCount = robot.CompletedVerificationCount = 3;
        simulation.Reset();
        robot = Assert.Single(simulation.Operators, op => op.IsRobot);
        Assert.Equal(0, robot.CatchCount);
        Assert.Equal(0, robot.CompletedVerificationCount);

        config.HallWidth = 15f;
        simulation.Reconfigure(config);
        Assert.True(Assert.Single(simulation.Operators).IsRobot);
        Assert.Equal(ResponderKind.WheeledRobot, Assert.Single(simulation.ResponderPosts).Kind);
        Assert.True(simulation.AddOperatorPost(new Vector2(7f, 5f)));
        Assert.Equal(2, simulation.Operators.Count);
    }

    [Fact]
    public void PlacementSpacingRemovalAndSeparateClearKeepKindsAligned()
    {
        CrowdSimulation simulation = CreateSimulation();
        Assert.True(simulation.AddOperatorPost(new Vector2(3f, 5f), ResponderKind.WheeledRobot));
        Assert.False(simulation.AddOperatorPost(new Vector2(3.2f, 5f)));
        Assert.False(simulation.AddOperatorPost(new Vector2(0.1f, 5f), ResponderKind.WheeledRobot));
        Assert.True(simulation.AddOperatorPost(new Vector2(5f, 5f)));
        Assert.True(simulation.RemoveNearestOperatorPost(new Vector2(3f, 5f)));
        Assert.False(Assert.Single(simulation.Operators).IsRobot);
        Assert.True(simulation.AddOperatorPost(new Vector2(3f, 5f), ResponderKind.WheeledRobot));
        simulation.ClearOperatorPosts(ResponderKind.PurpleOperator);
        Assert.True(Assert.Single(simulation.Operators).IsRobot);
        simulation.ClearOperatorPosts();
        Assert.Empty(simulation.ResponderPosts);
        Assert.True(simulation.AddOperatorPost(new Vector2(3f, 5f)));
        Assert.False(Assert.Single(simulation.Operators).IsRobot);
    }

    [Fact]
    public void SaveLoadRoundTripKeepsHumanAndRobotKindsAndLegacyFilesStayHuman()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"crowd-robot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            ScenarioConfigStore store = new(directory);
            ResponderPost[] posts =
            {
                new(new Vector2(2f, 2f), ResponderKind.PurpleOperator),
                new(new Vector2(5f, 5f), ResponderKind.WheeledRobot),
            };
            string path = store.Save("Mixed team", new SimulationConfig { RobotManualClearanceSeconds = 123f }, posts);
            LoadedScenarioConfig loaded = store.Load(Path.GetFileName(path));
            Assert.Equal(posts, loaded.ResponderPosts.ToArray());
            Assert.Contains("WheeledRobot", File.ReadAllText(path));
            Assert.DoesNotContain("HumanoidRobot", File.ReadAllText(path));
            Assert.Equal(123f, loaded.Config.RobotManualClearanceSeconds);
            Assert.Equal(123f, loaded.Config.Clone().RobotManualClearanceSeconds);
            Assert.Equal(posts.Select(post => post.Position).ToArray(), loaded.OperatorPosts.ToArray());
            CrowdSimulation simulation = new(loaded.Config);
            foreach (ResponderPost post in loaded.ResponderPosts)
            {
                Assert.True(simulation.AddOperatorPost(post.Position, post.Kind));
            }
            Assert.True(simulation.Operators[1].IsRobot);
            File.WriteAllText(Path.Combine(directory, "legacy.json"),
                """
                {"FormatVersion":1,"Scenario":{},"OperatorPosts":[{"X":2,"Y":2}]}
                """);
            Assert.Equal(ResponderKind.PurpleOperator,
                Assert.Single(store.Load("legacy.json").ResponderPosts).Kind);
            File.WriteAllText(Path.Combine(directory, "humanoid.json"),
                """
                {"FormatVersion":1,"Scenario":{},"OperatorPosts":[{"X":5,"Y":5,"Kind":"HumanoidRobot"}]}
                """);
            LoadedScenarioConfig migrated = store.Load("humanoid.json");
            Assert.Equal(new ResponderPost(new Vector2(5f, 5f), ResponderKind.WheeledRobot),
                Assert.Single(migrated.ResponderPosts));
            Assert.Equal(SimulationConfig.DefaultRobotManualClearanceSeconds, migrated.Config.RobotManualClearanceSeconds);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(10f)]
    [InlineData(180f)]
    public void RobotClearanceTimeAcceptsIndependentOneToOneHundredEightySeconds(float seconds)
    {
        SimulationConfig config = new() { RobotManualClearanceSeconds = seconds };
        config.Validate();
        CrowdSimulation simulation = new(config);
        Assert.True(simulation.AddOperatorPost(new Vector2(2f, 2f), ResponderKind.WheeledRobot));
        CheckpointOperator robot = Assert.Single(simulation.Operators);
        robot.CompletedVerificationCount = 100;
        Assert.Equal(seconds, robot.CurrentVerificationDuration);
        simulation.Reset();
        Assert.Equal(seconds, Assert.Single(simulation.Operators).CurrentVerificationDuration);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(180.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidRobotClearanceTimeIsRejected(float seconds) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SimulationConfig { RobotManualClearanceSeconds = seconds }.Validate());

    private static Pedestrian MakeDasher(CrowdSimulation simulation, int index, Vector2 position)
    {
        Pedestrian dasher = simulation.Pedestrians[index];
        Assert.True(simulation.TrySelectDasher(dasher.Id));
        dasher.Phase = PedestrianPhase.Room;
        dasher.DetectionStatus = DetectionStatus.DasherIncident;
        dasher.DasherState = DasherState.Dashing;
        dasher.Position = position;
        dasher.Velocity = Vector2.Zero;
        return dasher;
    }

    private static CrowdSimulation CreateSimulation() => new(new SimulationConfig
    {
        TargetFunnelDensity = 0.5f,
        YellowPercent = 0f, // Tests explicitly create Undetected targets; background flow stays Green.
        YellowVisualSearchSeconds = 5f,
    });

    private static Pedestrian MakeUndetected(CrowdSimulation simulation, int index, Vector2 position)
    {
        Pedestrian target = simulation.Pedestrians[index];
        target.Phase = PedestrianPhase.Room;
        target.DetectionStatus = DetectionStatus.CountedNotDetected;
        target.Position = position;
        target.Velocity = Vector2.Zero;
        target.ManualClearanceQueuedAtSeconds = 0f;
        return target;
    }

    private static Pedestrian PrepareReturningResponder(CrowdSimulation simulation, ResponderKind kind)
    {
        Assert.True(simulation.AddOperatorPost(new Vector2(3f, 5f), kind));
        CheckpointOperator responder = simulation.Operators[0];
        responder.State = OperatorState.Returning;
        responder.Position = new Vector2(6f, 5f);
        responder.Velocity = new Vector2(-1f, 0f);
        Pedestrian neighbour = simulation.Pedestrians[0];
        neighbour.Phase = PedestrianPhase.Room;
        neighbour.DetectionStatus = DetectionStatus.DetectedAndCounted;
        neighbour.Position = new Vector2(5.5f, 5f);
        neighbour.Velocity = Vector2.Zero;
        return neighbour;
    }
}
