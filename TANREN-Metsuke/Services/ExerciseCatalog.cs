using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

// NOTE: we must keep this in sync with TANREN Kiroku (Android), exerciseId values must match exactly
public static class ExerciseCatalog
{
    private static readonly List<ExerciseDefinition> exercises =
    [
        // Chest
        new() { Id = "bench_press", Name = "Flat Barbell Bench Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps } },

        new() { Id = "flat_dumbbell_bench_press", Name = "Flat Dumbbell Bench Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps } },

        new() { Id = "incline_barbell_bench_press", Name = "Incline Barbell Bench Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps } },

        new() { Id = "incline_dumbbell_bench_press", Name = "Incline Dumbbell Bench Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps } },

        new() { Id = "flat_dumbbell_fly", Name = "Flat Dumbbell Fly",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "incline_dumbbell_fly", Name = "Incline Dumbbell Fly",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "cable_crossover", Name = "Cable Crossover",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "cable_crossover_low_to_high", Name = "Cable Crossover Low to High",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "machine_press", Name = "Machine Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps } },

        new() { Id = "seated_machine_fly", Name = "Seated Machine Fly",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "push_up", Name = "Push Up",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps, MuscleGroup.Core } },

        new() { Id = "weighted_push_up", Name = "Weighted Push Up",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps, MuscleGroup.Core } },

        new() { Id = "decline_barbell_bench_press", Name = "Decline Barbell Bench Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "decline_dumbbell_bench_press", Name = "Decline Dumbbell Bench Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "decline_dumbbell_fly", Name = "Decline Dumbbell Fly",
            PrimaryMuscles = { MuscleGroup.Chest } },

        new() { Id = "cable_fly_high_to_low", Name = "Cable Fly High to Low",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "floor_press", Name = "Floor Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Triceps, MuscleGroup.Shoulders } },

        new() { Id = "dumbbell_floor_press", Name = "Dumbbell Floor Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Triceps, MuscleGroup.Shoulders } },

        new() { Id = "cable_chest_press", Name = "Cable Chest Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps } },

        new() { Id = "seated_cable_chest_fly", Name = "Seated Cable Chest Fly",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "smith_machine_bench_press", Name = "Smith Machine Bench Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps } },

        new() { Id = "smith_machine_incline_bench", Name = "Smith Machine Incline Bench Press",
            PrimaryMuscles = { MuscleGroup.Chest },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Triceps } },

        // Shoulders
        new() { Id = "overhead_press", Name = "Overhead Press",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Triceps, MuscleGroup.Core } },

        new() { Id = "seated_dumbbell_press", Name = "Seated Dumbbell Press",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "arnold_press", Name = "Arnold Press",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "lateral_raise", Name = "Lateral Raise",
            PrimaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "cable_lateral_raise", Name = "Cable Lateral Raise",
            PrimaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "rear_delt_fly", Name = "Rear Delt Fly",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Traps } },

        new() { Id = "face_pull", Name = "Face Pull",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Traps, MuscleGroup.Biceps } },

        new() { Id = "machine_shoulder_press", Name = "Machine Shoulder Press",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "dumbbell_front_raise", Name = "Dumbbell Front Raise",
            PrimaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "cable_front_raise", Name = "Cable Front Raise",
            PrimaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "machine_lateral_raise", Name = "Machine Lateral Raise",
            PrimaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "landmine_press", Name = "Landmine Press",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Triceps, MuscleGroup.Core } },

        new() { Id = "barbell_front_raise", Name = "Barbell Front Raise",
            PrimaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "barbell_rear_delt_row", Name = "Barbell Rear Delt Row",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Traps, MuscleGroup.Biceps } },

        new() { Id = "cable_rear_delt_row", Name = "Cable Rear Delt Row",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Traps, MuscleGroup.Biceps } },

        new() { Id = "push_press", Name = "Push Press",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Triceps, MuscleGroup.Core, MuscleGroup.Quads } },

        new() { Id = "reverse_machine_fly", Name = "Reverse Machine Fly",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Traps } },

        new() { Id = "behind_neck_press", Name = "Behind-the-Neck Press",
            PrimaryMuscles = { MuscleGroup.Shoulders },
            SecondaryMuscles = { MuscleGroup.Triceps } },

        // Triceps
        new() { Id = "tricep_pushdown", Name = "Tricep Pushdown",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "rope_pushdown", Name = "Rope Pushdown",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "close_grip_bench_press", Name = "Close Grip Bench Press",
            PrimaryMuscles = { MuscleGroup.Triceps },
            SecondaryMuscles = { MuscleGroup.Chest, MuscleGroup.Shoulders } },

        new() { Id = "dip", Name = "Dip",
            PrimaryMuscles = { MuscleGroup.Chest, MuscleGroup.Triceps },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "dumbbell_overhead_tricep_extension", Name = "Dumbbell Overhead Extension",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "ez_bar_skullcrusher", Name = "EZ-Bar Skullcrusher",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "dumbbell_skullcrusher", Name = "Dumbbell Skullcrusher",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "dumbbell_kickback", Name = "Dumbbell Kickback",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "diamond_push_up", Name = "Diamond Push Up",
            PrimaryMuscles = { MuscleGroup.Triceps },
            SecondaryMuscles = { MuscleGroup.Chest, MuscleGroup.Shoulders } },

        new() { Id = "barbell_skullcrusher", Name = "Barbell Skullcrusher",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "cable_overhead_extension", Name = "Cable Overhead Tricep Extension",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "single_arm_pushdown", Name = "Single-Arm Pushdown",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "bench_dip", Name = "Bench Dip",
            PrimaryMuscles = { MuscleGroup.Triceps },
            SecondaryMuscles = { MuscleGroup.Chest, MuscleGroup.Shoulders } },

        new() { Id = "crossbody_cable_extension", Name = "Cross-Body Cable Triceps Extension",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "machine_overhead_extension", Name = "Machine Overhead Triceps Extension",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        new() { Id = "tate_press", Name = "Tate Press",
            PrimaryMuscles = { MuscleGroup.Triceps } },

        // Biceps
        new() { Id = "bicep_curl", Name = "Barbell Curl",
            PrimaryMuscles = { MuscleGroup.Biceps },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "dumbbell_curl", Name = "Dumbbell Curl",
            PrimaryMuscles = { MuscleGroup.Biceps },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "hammer_curl", Name = "Hammer Curl",
            PrimaryMuscles = { MuscleGroup.Biceps },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "ez_bar_curl", Name = "EZ-Bar Curl",
            PrimaryMuscles = { MuscleGroup.Biceps },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "ez_bar_preacher_curl", Name = "EZ-Bar Preacher Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "concentration_curl", Name = "Concentration Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "cable_bicep_curl", Name = "Cable Curl",
            PrimaryMuscles = { MuscleGroup.Biceps },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "incline_dumbbell_curl", Name = "Incline Dumbbell Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "spider_curl", Name = "Spider Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "machine_curl", Name = "Machine Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "dumbbell_preacher_curl", Name = "Dumbbell Preacher Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "cross_body_hammer_curl", Name = "Cross-Body Hammer Curl",
            PrimaryMuscles = { MuscleGroup.Biceps },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "barbell_preacher_curl", Name = "Barbell Preacher Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "drag_curl", Name = "Drag Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "overhead_cable_curl", Name = "Overhead Cable Curl",
            PrimaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "cable_curl_rope", Name = "Cable Curl with Rope",
            PrimaryMuscles = { MuscleGroup.Biceps },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        // Back (lats)
        new() { Id = "barbell_row", Name = "Barbell Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Forearms } },

        new() { Id = "dumbbell_row", Name = "Dumbbell Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Forearms } },

        new() { Id = "seated_cable_row", Name = "Seated Cable Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "t_bar_row", Name = "T-Bar Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Forearms } },

        new() { Id = "lat_pulldown", Name = "Lat Pulldown",
            PrimaryMuscles = { MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "pull_up", Name = "Pull Ups",
            PrimaryMuscles = { MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Core } },

        new() { Id = "chin_up", Name = "Chin-Up",
            PrimaryMuscles = { MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Core } },

        new() { Id = "deadlift", Name = "Deadlift",
            PrimaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Traps, MuscleGroup.Core, MuscleGroup.Forearms } },

        new() { Id = "chest_supported_row", Name = "Chest-Supported Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "pendlay_row", Name = "Pendlay Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Core } },

        new() { Id = "machine_row", Name = "Machine Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "single_arm_cable_row", Name = "Single-Arm Cable Row",
            PrimaryMuscles = { MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Core } },

        new() { Id = "straight_arm_pulldown", Name = "Straight Arm Pulldown",
            PrimaryMuscles = { MuscleGroup.Back } },

        new() { Id = "dumbbell_pullover", Name = "Dumbbell Pullover",
            PrimaryMuscles = { MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Chest, MuscleGroup.Triceps } },

        new() { Id = "neutral_grip_pulldown", Name = "Neutral Grip Lat Pulldown",
            PrimaryMuscles = { MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "close_grip_lat_pulldown", Name = "Close-Grip Lat Pulldown",
            PrimaryMuscles = { MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "back_extension", Name = "Back Extension",
            PrimaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Back, MuscleGroup.Core } },

        new() { Id = "gorilla_row", Name = "Gorilla Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Core } },

        new() { Id = "inverted_row", Name = "Inverted Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps, MuscleGroup.Core } },

        new() { Id = "kettlebell_swing", Name = "Kettlebell Swing",
            PrimaryMuscles = { MuscleGroup.Glutes, MuscleGroup.Hamstrings },
            SecondaryMuscles = { MuscleGroup.Core, MuscleGroup.Back, MuscleGroup.Shoulders } },

        new() { Id = "renegade_row", Name = "Renegade Row",
            PrimaryMuscles = { MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Core, MuscleGroup.Biceps, MuscleGroup.Triceps } },

        new() { Id = "seal_row", Name = "Seal Row",
            PrimaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "trap_bar_deadlift", Name = "Trap Bar Deadlift",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes, MuscleGroup.Hamstrings },
            SecondaryMuscles = { MuscleGroup.Back, MuscleGroup.Traps, MuscleGroup.Core } },

        // Traps
        new() { Id = "shrug", Name = "Shrug",
            PrimaryMuscles = { MuscleGroup.Traps } },

        new() { Id = "dumbbell_shrug", Name = "Dumbbell Shrug",
            PrimaryMuscles = { MuscleGroup.Traps } },

        new() { Id = "upright_row", Name = "Upright Row",
            PrimaryMuscles = { MuscleGroup.Traps },
            SecondaryMuscles = { MuscleGroup.Shoulders, MuscleGroup.Biceps } },

        new() { Id = "cable_shrug", Name = "Cable Shrug",
            PrimaryMuscles = { MuscleGroup.Traps } },

        new() { Id = "rack_pull", Name = "Rack Pull",
            PrimaryMuscles = { MuscleGroup.Traps, MuscleGroup.Back },
            SecondaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes, MuscleGroup.Forearms } },

        // Forearms
        new() { Id = "wrist_curl", Name = "Wrist Curl",
            PrimaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "reverse_wrist_curl", Name = "Reverse Wrist Curl",
            PrimaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "reverse_barbell_curl", Name = "Reverse Barbell Curl",
            PrimaryMuscles = { MuscleGroup.Forearms },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "zottman_curl", Name = "Zottman Curl",
            PrimaryMuscles = { MuscleGroup.Biceps },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "farmers_carry", Name = "Farmer's Carry",
            PrimaryMuscles = { MuscleGroup.Forearms },
            SecondaryMuscles = { MuscleGroup.Traps, MuscleGroup.Core } },

        new() { Id = "dead_hang", Name = "Dead Hang",
            PrimaryMuscles = { MuscleGroup.Forearms },
            SecondaryMuscles = { MuscleGroup.Back } },

        new() { Id = "reverse_dumbbell_curl", Name = "Reverse Dumbbell Curl",
            PrimaryMuscles = { MuscleGroup.Forearms },
            SecondaryMuscles = { MuscleGroup.Biceps } },

        new() { Id = "gripper", Name = "Gripper",
            PrimaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "plate_pinch", Name = "Plate Pinch",
            PrimaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "wrist_roller", Name = "Wrist Roller",
            PrimaryMuscles = { MuscleGroup.Forearms } },

        // Core
        new() { Id = "plank", Name = "Plank",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "crunch", Name = "Crunch",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "situp", Name = "Situp",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "cable_crunch", Name = "Cable Crunch",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "hanging_leg_raise", Name = "Hanging Leg Raise",
            PrimaryMuscles = { MuscleGroup.Core },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "ab_wheel_rollout", Name = "Ab Wheel Rollout",
            PrimaryMuscles = { MuscleGroup.Core },
            SecondaryMuscles = { MuscleGroup.Back } },

        new() { Id = "russian_twist", Name = "Russian Twist",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "bicycle_crunch", Name = "Bicycle Crunch",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "side_plank", Name = "Side Plank",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "lying_leg_raise", Name = "Lying Leg Raise",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "dragon_flag", Name = "Dragon Flag",
            PrimaryMuscles = { MuscleGroup.Core },
            SecondaryMuscles = { MuscleGroup.Back } },

        new() { Id = "pallof_press", Name = "Pallof Press",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "decline_crunch", Name = "Decline Crunch",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "captains_chair_knee_raise", Name = "Captain's Chair Knee Raise",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "captains_chair_leg_raise", Name = "Captain's Chair Leg Raise",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "copenhagen_plank", Name = "Copenhagen Plank",
            PrimaryMuscles = { MuscleGroup.Core },
            SecondaryMuscles = { MuscleGroup.Quads } },

        new() { Id = "dumbbell_side_bend", Name = "Dumbbell Side Bend",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "hanging_knee_raise", Name = "Hanging Knee Raise",
            PrimaryMuscles = { MuscleGroup.Core },
            SecondaryMuscles = { MuscleGroup.Forearms } },

        new() { Id = "hanging_windshield_wiper", Name = "Hanging Windshield Wiper",
            PrimaryMuscles = { MuscleGroup.Core },
            SecondaryMuscles = { MuscleGroup.Forearms, MuscleGroup.Back } },

        new() { Id = "cable_wood_chop", Name = "Cable Wood Chop",
            PrimaryMuscles = { MuscleGroup.Core },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "landmine_rotation", Name = "Landmine Rotation",
            PrimaryMuscles = { MuscleGroup.Core },
            SecondaryMuscles = { MuscleGroup.Shoulders } },

        new() { Id = "lying_windshield_wiper", Name = "Lying Windshield Wiper",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "machine_crunch", Name = "Machine Crunch",
            PrimaryMuscles = { MuscleGroup.Core } },

        new() { Id = "oblique_crunch", Name = "Oblique Crunch",
            PrimaryMuscles = { MuscleGroup.Core } },

        // Quads
        new() { Id = "squat", Name = "Squat",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Core } },

        new() { Id = "front_squat", Name = "Front Squat",
            PrimaryMuscles = { MuscleGroup.Quads },
            SecondaryMuscles = { MuscleGroup.Core, MuscleGroup.Glutes } },

        new() { Id = "goblet_squat", Name = "Goblet Squat",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Core } },

        new() { Id = "hack_squat", Name = "Hack Squat",
            PrimaryMuscles = { MuscleGroup.Quads },
            SecondaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "leg_press", Name = "Leg Press",
            PrimaryMuscles = { MuscleGroup.Quads },
            SecondaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "leg_extension", Name = "Leg Extension",
            PrimaryMuscles = { MuscleGroup.Quads } },

        new() { Id = "bulgarian_split_squat", Name = "Bulgarian Split Squat",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Core } },

        new() { Id = "lunge", Name = "Lunge",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "step_up", Name = "Step Up",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "sissy_squat", Name = "Sissy Squat",
            PrimaryMuscles = { MuscleGroup.Quads } },

        new() { Id = "pistol_squat", Name = "Pistol Squat",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Core } },

        new() { Id = "walking_lunge", Name = "Walking Lunge",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "reverse_lunge", Name = "Reverse Lunge",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "cyclist_squat", Name = "Cyclist Squat",
            PrimaryMuscles = { MuscleGroup.Quads } },

        new() { Id = "barbell_hack_squat", Name = "Barbell Hack Squat",
            PrimaryMuscles = { MuscleGroup.Quads },
            SecondaryMuscles = { MuscleGroup.Glutes, MuscleGroup.Hamstrings } },

        new() { Id = "belt_squat", Name = "Belt Squat",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "box_squat", Name = "Box Squat",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Core } },

        new() { Id = "curtsy_lunge", Name = "Curtsy Lunge",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "hip_adduction_machine", Name = "Hip Adduction Machine",
            PrimaryMuscles = { MuscleGroup.Quads } },

        new() { Id = "pendulum_squat", Name = "Pendulum Squat",
            PrimaryMuscles = { MuscleGroup.Quads },
            SecondaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "safety_bar_squat", Name = "Safety Bar Squat",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Core } },

        new() { Id = "lateral_lunge", Name = "Lateral Lunge",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "smith_machine_squat", Name = "Smith Machine Squat",
            PrimaryMuscles = { MuscleGroup.Quads, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Core } },

        // Hamstrings
        new() { Id = "leg_curl", Name = "Leg Curl",
            PrimaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "seated_leg_curl", Name = "Seated Leg Curl",
            PrimaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "romanian_deadlift", Name = "Romanian Deadlift",
            PrimaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Back, MuscleGroup.Core } },

        new() { Id = "romanian_dumbbell_deadlift", Name = "Romanian Dumbbell Deadlift",
            PrimaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Core } },

        new() { Id = "stiff_leg_deadlift", Name = "Stiff-Leg Deadlift",
            PrimaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Back, MuscleGroup.Core } },

        new() { Id = "nordic_curl", Name = "Nordic Curl",
            PrimaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "good_morning", Name = "Good Morning",
            PrimaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Core, MuscleGroup.Back } },

        new() { Id = "sumo_deadlift", Name = "Sumo Deadlift",
            PrimaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Back, MuscleGroup.Core, MuscleGroup.Forearms } },

        new() { Id = "single_leg_rdl", Name = "Single-Leg Romanian Deadlift",
            PrimaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Core } },

        new() { Id = "glute_ham_raise", Name = "Glute-Ham Raise",
            PrimaryMuscles = { MuscleGroup.Hamstrings },
            SecondaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "reverse_nordic", Name = "Reverse Nordic",
            PrimaryMuscles = { MuscleGroup.Quads },
            SecondaryMuscles = { MuscleGroup.Core } },

        // Glutes
        new() { Id = "glute_bridge", Name = "Glute Bridge",
            PrimaryMuscles = { MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "barbell_hip_thrust", Name = "Barbell Hip Thrust",
            PrimaryMuscles = { MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "cable_glute_kickback", Name = "Cable Glute Kickback",
            PrimaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "donkey_kickback", Name = "Donkey Kickback",
            PrimaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "sumo_squat", Name = "Sumo Squat",
            PrimaryMuscles = { MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Quads, MuscleGroup.Hamstrings } },

        new() { Id = "hip_abduction_machine", Name = "Hip Abduction Machine",
            PrimaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "cable_pull_through", Name = "Cable Pull-Through",
            PrimaryMuscles = { MuscleGroup.Glutes, MuscleGroup.Hamstrings },
            SecondaryMuscles = { MuscleGroup.Core } },

        new() { Id = "lateral_band_walk", Name = "Lateral Band Walk",
            PrimaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "frog_pump", Name = "Frog Pump",
            PrimaryMuscles = { MuscleGroup.Glutes } },

        new() { Id = "dumbbell_hip_thrust", Name = "Dumbbell Hip Thrust",
            PrimaryMuscles = { MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "single_leg_hip_thrust", Name = "Single-Leg Hip Thrust",
            PrimaryMuscles = { MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "single_leg_glute_bridge", Name = "Single-Leg Glute Bridge",
            PrimaryMuscles = { MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "hip_thrust_machine", Name = "Hip Thrust Machine",
            PrimaryMuscles = { MuscleGroup.Glutes },
            SecondaryMuscles = { MuscleGroup.Hamstrings } },

        new() { Id = "reverse_hyperextension", Name = "Reverse Hyperextension",
            PrimaryMuscles = { MuscleGroup.Glutes, MuscleGroup.Hamstrings },
            SecondaryMuscles = { MuscleGroup.Core } },

        new() { Id = "cossack_squat", Name = "Cossack Squat",
            PrimaryMuscles = { MuscleGroup.Glutes, MuscleGroup.Quads },
            SecondaryMuscles = { MuscleGroup.Hamstrings, MuscleGroup.Core } },

        // Calves
        new() { Id = "calf_raise", Name = "Calf Raise",
            PrimaryMuscles = { MuscleGroup.Calves } },

        new() { Id = "seated_calf_raise", Name = "Seated Calf Raise",
            PrimaryMuscles = { MuscleGroup.Calves } },

        new() { Id = "single_leg_calf_raise", Name = "Single-Leg Calf Raise",
            PrimaryMuscles = { MuscleGroup.Calves } },

        new() { Id = "single_leg_seated_calf_raise", Name = "Single-Leg Seated Calf Raise",
            PrimaryMuscles = { MuscleGroup.Calves } },

        new() { Id = "donkey_calf_raise", Name = "Donkey Calf Raise",
            PrimaryMuscles = { MuscleGroup.Calves } },

        new() { Id = "leg_press_calf_raise", Name = "Leg Press Calf Raise",
            PrimaryMuscles = { MuscleGroup.Calves } },

        new() { Id = "barbell_calf_raise", Name = "Barbell Calf Raise",
            PrimaryMuscles = { MuscleGroup.Calves } },

        // Shins
        new() { Id = "tibialis_wall_raise", Name = "Tibialis Wall Raise",
            PrimaryMuscles = { MuscleGroup.Shins } },

        new() { Id = "tib_bar_raise", Name = "Tib-Bar Raise",
            PrimaryMuscles = { MuscleGroup.Shins } },
    ];

    private static readonly Dictionary<string, ExerciseDefinition> builtinById = exercises.ToDictionary(e => e.Id);
    private static Dictionary<string, ExerciseDefinition> lookup = builtinById;
    private static IReadOnlyList<ExerciseDefinition> all = exercises;

    public static ExerciseDefinition? Get(string id) => lookup.GetValueOrDefault(id);

    public static IReadOnlyList<ExerciseDefinition> All => all;

    // loads custom exercises from a JSON file, if it doesn't exist or is invalid, it falls back to the default exercises only
    public static void LoadCustomExercises()
    {
        var path = Path.Combine(SettingsService.WorkoutsFolder, "custom_exercises.json");
        List<ExerciseDefinition> customExercises = [];
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var dtos = JsonSerializer.Deserialize<List<CustomExerciseDto>>(json, JsonDefaults.CaseInsensitive) ?? [];
                customExercises = [.. dtos.Select(dto => new ExerciseDefinition
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    PrimaryMuscles = [.. dto.PrimaryMuscles.Select(ParseMuscle).Where(m => m.HasValue).Select(m => m!.Value)],
                    SecondaryMuscles = [.. dto.SecondaryMuscles.Select(ParseMuscle).Where(m => m.HasValue).Select(m => m!.Value)]
                })];
            }
            catch
            {
                customExercises = [];
            }
        }

        if (customExercises.Count == 0)
        {
            all = exercises;
            lookup = builtinById;
            return;
        }

        all = [.. exercises, .. customExercises];
        // we add customs first then builtins, so that a builtin id always wins on collision
        var merged = new Dictionary<string, ExerciseDefinition>();
        foreach (var e in customExercises)
            merged[e.Id] = e;
        foreach (var e in exercises)
            merged[e.Id] = e;
        lookup = merged;
    }

    private static MuscleGroup? ParseMuscle(string name) => Enum.TryParse<MuscleGroup>(name, true, out var g) ? g : null;
}

file class CustomExerciseDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> PrimaryMuscles { get; set; } = [];
    public List<string> SecondaryMuscles { get; set; } = [];
}
