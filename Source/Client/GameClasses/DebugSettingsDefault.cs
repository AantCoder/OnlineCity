using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorldOnlineCity.GameClasses
{
    public static class DebugSettingsDefault
    {
        public static void SetDefault()
		{
			DebugViewSettings.drawFog = true;

			DebugViewSettings.drawSnow = true;

			DebugViewSettings.drawTerrain = true;

			DebugViewSettings.drawTerrainWater = true;

			DebugViewSettings.drawThingsDynamic = true;

			DebugViewSettings.drawThingsPrinted = true;

			DebugViewSettings.drawShadows = true;

			DebugViewSettings.drawLightingOverlay = true;

			DebugViewSettings.drawWorldOverlays = true;

			DebugViewSettings.drawPaths = false;

			DebugViewSettings.drawCastPositionSearch = false;

			DebugViewSettings.drawDestSearch = false;

			DebugViewSettings.drawStyleSearch = false;

			DebugViewSettings.drawSectionEdges = false;

			DebugViewSettings.drawRiverDebug = false;

			DebugViewSettings.drawPawnDebug = false;

			DebugViewSettings.drawPawnRotatorTarget = false;

			DebugViewSettings.drawRegions = false;

			DebugViewSettings.drawRegionLinks = false;

			DebugViewSettings.drawRegionDirties = false;

			DebugViewSettings.drawRegionTraversal = false;

			DebugViewSettings.drawRegionThings = false;

			DebugViewSettings.drawDistricts = false;

			DebugViewSettings.drawRooms = false;

			DebugViewSettings.drawPower = false;

			DebugViewSettings.drawPowerNetGrid = false;

			DebugViewSettings.drawOpportunisticJobs = false;

			DebugViewSettings.drawTooltipEdges = false;

			DebugViewSettings.drawRecordedNoise = false;

			DebugViewSettings.drawFoodSearchFromMouse = false;

			DebugViewSettings.drawPreyInfo = false;

			DebugViewSettings.drawGlow = false;

			DebugViewSettings.drawAvoidGrid = false;

			DebugViewSettings.drawBreachingGrid = false;

			DebugViewSettings.drawBreachingNoise = false;

			DebugViewSettings.drawLords = false;

			DebugViewSettings.drawDuties = false;

			DebugViewSettings.drawShooting = false;

			DebugViewSettings.drawInfestationChance = false;

			DebugViewSettings.drawStealDebug = false;

			DebugViewSettings.drawDeepResources = false;

			DebugViewSettings.drawAttackTargetScores = false;

			DebugViewSettings.drawInteractionCells = false;

			DebugViewSettings.drawDoorsDebug = false;

			DebugViewSettings.drawDestReservations = false;

			DebugViewSettings.drawDamageRects = false;

			DebugViewSettings.writeGame = false;

			DebugViewSettings.writeSteamItems = false;

			DebugViewSettings.writeConcepts = false;

			DebugViewSettings.writeReservations = false;

			DebugViewSettings.writePathCosts = false;

			DebugViewSettings.writeFertility = false;

			DebugViewSettings.writeLinkFlags = false;

			DebugViewSettings.writeCover = false;

			DebugViewSettings.writeCellContents = false;

			DebugViewSettings.writeMusicManagerPlay = false;

			DebugViewSettings.writeStoryteller = false;

			DebugViewSettings.writePlayingSounds = false;

			DebugViewSettings.writeSoundEventsRecord = false;

			DebugViewSettings.writeMoteSaturation = false;

			DebugViewSettings.writeSnowDepth = false;

			DebugViewSettings.writeEcosystem = false;

			DebugViewSettings.writeRecentStrikes = false;

			DebugViewSettings.writeBeauty = false;

			DebugViewSettings.writeListRepairableBldgs = false;

			DebugViewSettings.writeListFilthInHomeArea = false;

			DebugViewSettings.writeListHaulables = false;

			DebugViewSettings.writeListMergeables = false;

			DebugViewSettings.writeTotalSnowDepth = false;

			DebugViewSettings.writeCanReachColony = false;

			DebugViewSettings.writeMentalStateCalcs = false;

			DebugViewSettings.writeWind = false;

			DebugViewSettings.writeTerrain = false;

			DebugViewSettings.writeApparelScore = false;

			DebugViewSettings.writeWorkSettings = false;

			DebugViewSettings.writeSkyManager = false;

			DebugViewSettings.writeMemoryUsage = false;

			DebugViewSettings.writeMapGameConditions = false;

			DebugViewSettings.writeAttackTargets = false;

			DebugViewSettings.writeRopesAndPens = false;

			DebugViewSettings.writeRoomRoles = false;

			DebugViewSettings.logIncapChance = false;

			DebugViewSettings.logInput = false;

			DebugViewSettings.logApparelGeneration = false;

			DebugViewSettings.logLordToilTransitions = false;

			DebugViewSettings.logGrammarResolution = false;

			DebugViewSettings.logCombatLogMouseover = false;

			DebugViewSettings.logCauseOfDeath = false;

			DebugViewSettings.logMapLoad = false;

			DebugViewSettings.logTutor = false;

			DebugViewSettings.logSignals = false;

			DebugViewSettings.logWorldPawnGC = false;

			DebugViewSettings.logTaleRecording = false;

			DebugViewSettings.logHourlyScreenshot = false;

			DebugViewSettings.logFilthSummary = false;

			DebugViewSettings.logCarriedBetweenJobs = false;

			DebugViewSettings.logComplexGenPoints = false;

			DebugViewSettings.debugApparelOptimize = false;

			DebugViewSettings.showAllRoomStats = false;

			DebugViewSettings.showFloatMenuWorkGivers = false;

			DebugViewSettings.neverForceNormalSpeed = false;

			DebugSettings.enableDamage = true;

			DebugSettings.enablePlayerDamage = true;

			DebugSettings.enableRandomMentalStates = true;

			DebugSettings.enableStoryteller = true;

			DebugSettings.enableRandomDiseases = true;

			DebugSettings.godMode = false;

			DebugSettings.noAnimals = false;

			DebugSettings.unlimitedPower = false;

			DebugSettings.pathThroughWalls = false;

			DebugSettings.instantRecruit = false;

			DebugSettings.alwaysSocialFight = false;

			DebugSettings.alwaysDoLovin = false;

			DebugSettings.detectRegionListersBugs = false;

			DebugSettings.instantVisitorsGift = false;

			DebugSettings.lowFPS = false;

			DebugSettings.fastResearch = false;

			DebugSettings.fastLearning = false;

			DebugSettings.fastEcology = false;

			DebugSettings.fastEcologyRegrowRateOnly = false;

			DebugSettings.fastCrafting = false;

			DebugSettings.fastCaravans = false;

			DebugSettings.activateAllBuildingDemands = false;

			DebugSettings.activateAllIdeoRoles = false;

			DebugSettings.showLocomotionUrgency = false;

			DebugSettings.playRitualAmbience = true;

		}

    }
}
