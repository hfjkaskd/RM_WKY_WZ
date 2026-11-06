"""Run existing formal-scene checks in a private Unity batch project, without UI input.

The initial copy seeds Unity's import cache (~7 GB total including source assets).
Subsequent runs copy changed source files only. The working editor and .git are
never modified by the batch editor. No symlinks/hardlinks or shared writable cache.
"""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
import shutil
import subprocess
import time

PROJECT = Path(__file__).resolve().parent.parent
MIRROR = PROJECT.parent / "BackgroundUnityVerification"
UNITY = Path("C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Unity.exe")
REPORTS = {
    "game-start": "live_game_start_verification.json",
    "initial-population": "live_player_input_verification.json",
    "possible-moves": "live_player_input_verification.json",
    "generated-counts": "live_player_input_verification.json",
    "game-scene": "live_player_input_verification.json",
    "camera-projection": "live_player_input_verification.json",
    "square-background": "live_square_background_verification.json",
    "core-catalog": "live_player_input_verification.json",
    "core-ui-author": "core_ui_authoring.json",
    "core-flow": "live_core_flow_verification.json",
    "core-flow-back": "live_core_flow_verification.json",
    "core-attempt-resume": "live_core_attempt_resume_verification.json",
    "core-opening-checkpoint": "live_core_opening_checkpoint_verification.json",
    "core-authored-openings": "live_core_authored_openings_verification.json",
    "core-natural-attempts": "live_core_natural_attempts_verification.json",
    "core-dialog-background": "live_core_dialog_background_verification.json",
    "core-level-settings": "live_core_level_settings_verification.json",
    "core-modal-tutorial-author": "core_modal_tutorial_authoring.json",
    "core-directed-tutorial-author": "core_directed_tutorial_authoring.json",
    "core-modal-tutorials": "live_core_modal_tutorials_verification.json",
    "core-directed-tutorials": "live_core_directed_tutorials_verification.json",
    "core-optional-tutorials": "live_core_optional_tutorials_verification.json",
    "core-match-hints": "live_core_match_hint_verification.json",
    "core-power-hints": "live_core_power_hint_verification.json",
    "core-progress-pause": "live_core_power_hint_verification.json",
    "core-king-worry": "live_core_king_worry_verification.json",
    "core-king-mail-worry": "live_core_king_mail_worry_verification.json",
    "core-king-safe-worry": "live_core_king_safe_worry_verification.json",
    "core-king-combo": "live_core_king_combo_verification.json",
    "core-king-ballball": "live_core_king_ballball_verification.json",
    "core-king-lightball-tnt": "live_core_king_lightball_tnt_verification.json",
    "core-lightball-tnt-overlap": "live_core_lightball_tnt_overlap_verification.json",
    "core-king-lightball-propeller": "live_core_king_lightball_propeller_verification.json",
    "core-king-lightball-rocket": "live_core_king_lightball_rocket_verification.json",
    "core-king-propeller-single": "live_core_king_propeller_single_verification.json",
    "core-king-propeller-pair": "live_core_king_propeller_pair_verification.json",
    "core-king-propeller-tnt": "live_core_king_propeller_tnt_verification.json",
    "core-king-propeller-rocket": "live_core_king_propeller_rocket_verification.json",
    "core-hammer": "live_core_hammer_verification.json",
    "core-arrow": "live_core_arrow_verification.json",
    "core-cannon": "live_core_cannon_verification.json",
    "core-jester": "live_core_jester_verification.json",
    "core-lightball-tap-guide": "live_core_lightball_tap_guide_verification.json",
    "core-dynamic-power-guides": "live_core_dynamic_power_tutorial_verification.json",
    "core-recolor-shuffle": "live_core_recolor_shuffle_verification.json",
    "core-no-moves-author": "core_no_moves_authoring.json",
    "core-no-moves": "live_core_no_moves_verification.json",
    "core-box-chapter": "live_core_box_chapter_verification.json",
    "core-grass": "live_grass_gameplay_verification.json",
    "core-grass-chain": "live_core_grass_chain_verification.json",
    "core-cupboard": "live_core_cupboard_verification.json",
    "core-cupboard-footprint": "live_core_cupboard_footprint_verification.json",
    "core-potion-footprint": "live_core_cupboard_footprint_verification.json",
    "core-early-chapter": "live_core_cupboard_verification.json",
    "core-random-chapter": "live_core_random_chapter_verification.json",
    "core-random-opening": "live_core_random_chapter_verification.json",
    "core-coin": "live_core_coin_verification.json",
    "core-mail": "live_core_mail_verification.json",
    "core-mail-batch": "live_core_mail_batch_verification.json",
    "core-mail-target-score": "live_core_mail_target_score_verification.json",
    "core-guided-score-cache": "live_core_guided_score_cache_verification.json",
    "core-mixed-chapter": "live_core_mixed_chapter_verification.json",
    "core-egg-chapter": "live_core_egg_chapter_verification.json",
    "core-potion-chapter": "live_core_potion_chapter_verification.json",
    "core-vase-chapter": "live_core_vase_chapter_verification.json",
    "core-owl-chapter": "live_core_owl_chapter_verification.json",
    "core-bush-chapter": "live_core_bush_chapter_verification.json",
    "core-bush-coin-opening": "live_core_bush_chapter_verification.json",
    "core-bush-burst": "live_core_bush_burst_verification.json",
    "core-honey-chapter": "live_core_honey_chapter_verification.json",
    "core-honey-opening": "live_core_honey_chapter_verification.json",
    "core-honey-vase-reveal": "live_core_honey_vase_reveal_verification.json",
    "core-merge-first-frame": "live_core_merge_first_frame_verification.json",
    "core-safe-chapter": "live_core_safe_chapter_verification.json",
    "core-safe-opening": "live_core_safe_chapter_verification.json",
    "core-level100": "live_core_safe_chapter_verification.json",
    "life-helper": "live_player_input_verification.json",
    "life-clock-win": "live_remaining_coins_normal_verification.json",
    "area-data": "live_area_data_verification.json",
    "lightball-presentation": "lightball_use_presentation_verification.json",
    "player-input": "live_player_input_verification.json",
    "ego-continuation": "live_ego_continuation_verification.json",
    "propeller-targets": "live_propeller_targets_verification.json",
    "propeller-tap": "live_propeller_tap_verification.json",
    "propeller-swap": "live_propeller_swap_verification.json",
    "tnt-tap": "live_tnt_tap_verification.json",
    "tnt-swap": "live_tnt_swap_verification.json",
    "tnt-tnt-combo": "live_tnt_tnt_combo_verification.json",
    "rocket-rocket-combo": "live_rocket_rocket_combo_verification.json",
    "tnt-rocket-combo": "live_tnt_rocket_combo_verification.json",
    "goal-ui": "live_goal_ui_verification.json",
    "win-logo-visual": "live_win_logo_visual_verification.json",
    "win-logo-skip": "live_remaining_coins_logo_skip_verification.json",
    "remaining-moves-exploder": "live_remaining_moves_exploder_verification.json",
    "remaining-moves-replacement": "live_remaining_moves_replacement_verification.json",
    "remaining-coins-normal": "live_remaining_coins_normal_verification.json",
    "remaining-coins-skip": "live_remaining_coins_skip_verification.json",
    "remaining-coins-early-skip": "live_remaining_coins_early_skip_verification.json",
    "propeller-pair-combo": "live_propeller_pair_combo_verification.json",
    "tnt-propeller-combo": "live_tnt_propeller_combo_verification.json",
    "ball-ball-combo": "live_ball_ball_combo_verification.json",
    "propeller-rocket-combo": "live_propeller_rocket_combo_verification.json",
    "lightball-tnt-combo": "live_lightball_tnt_combo_verification.json",
    "lightball-rocket-combo": "live_lightball_rocket_combo_verification.json",
    "lightball-combos": "live_lightball_combos_verification.json",
    "lightball-propeller-combo": "live_lightball_propeller_combo_verification.json",
    "lightball-combo-regression": "live_lightball_combo_regression_verification.json",
    "lightball-tap": "live_lightball_tap_verification.json",
    "lightball-swap": "live_lightball_swap_verification.json",
    "rocket-horizontal-tap": "live_rocket_horizontal_tap_verification.json",
    "rocket-horizontal-swap": "live_rocket_horizontal_swap_verification.json",
    "rocket-vertical-tap": "live_rocket_vertical_tap_verification.json",
    "rocket-vertical-swap": "live_rocket_vertical_swap_verification.json",
}


def source_files():
    return sorted(f for root in ("Assets", "Packages", "ProjectSettings")
                  for f in (PROJECT / root).rglob("*") if f.is_file())


def fingerprint(files):
    def file_digest(path):
        value = hashlib.sha256()
        with path.open("rb") as stream:
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                value.update(block)
        return value.digest()

    digest = hashlib.sha256()
    with ThreadPoolExecutor(max_workers=4) as pool:
        for path, value in zip(files, pool.map(file_digest, files)):
            digest.update(path.relative_to(PROJECT).as_posix().encode("utf-8") + b"\0" + value)
    return digest.hexdigest()


def sync_one(source):
    target = MIRROR / source.relative_to(PROJECT)
    a = source.stat()
    if target.exists():
        b = target.stat()
        if a.st_size == b.st_size and a.st_mtime_ns == b.st_mtime_ns:
            return 0
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)
    return a.st_size


def main():
    global MIRROR
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("test", choices=REPORTS)
    parser.add_argument("--mirror-path", type=Path,
                        help="Use an empty directory or an existing verification mirror on another drive.")
    parser.add_argument("--diagnostic-only", action="store_true",
                        help="Incremental debugging only; skip expensive full-source hashing, never produce canonical proof.")
    parser.add_argument("--opening-levels", help="Ordered comma-separated levels for an authored opening or natural attempt run.")
    parser.add_argument("--resume-level", type=int, help="Numbered level for the attempt or opening checkpoint verification.")
    parser.add_argument("--summary-path", type=Path, help="Write the suite summary to a separate path.")
    parser.add_argument("--timeout-seconds", type=int,
                        help="Override the editor verification time limit for a longer run.")
    args = parser.parse_args()
    if args.timeout_seconds is not None and not 0 < args.timeout_seconds <= 2_147_483_647:
        parser.error("--timeout-seconds must be a positive 32-bit integer")
    if (args.test in ("core-authored-openings", "core-natural-attempts")) != bool(args.opening_levels):
        parser.error("--opening-levels is required only for authored openings and natural attempts")
    opening_checkpoint = args.test == "core-opening-checkpoint"
    if args.resume_level is not None and (args.test not in ("core-attempt-resume", "core-opening-checkpoint") or
                                          not (1 if opening_checkpoint else 2) <= args.resume_level <= 100):
        parser.error("--resume-level requires a supported level for the selected checkpoint verification")
    if args.mirror_path is not None:
        mirror = args.mirror_path.resolve()
        project = PROJECT.resolve()
        if mirror == project or project in mirror.parents or mirror in project.parents:
            parser.error("--mirror-path must be separate from the working Unity project")
        if mirror.exists() and (not mirror.is_dir() or
                                (not (mirror / ".cache_seeded").is_file() and any(mirror.iterdir()))):
            parser.error("--mirror-path must be empty or contain a seeded verification mirror")
        MIRROR = mirror
    MIRROR.mkdir(parents=True, exist_ok=True)
    cache_ready = MIRROR / ".cache_seeded"
    minimum_gib = 1 if cache_ready.is_file() else 8
    if shutil.disk_usage(MIRROR).free < minimum_gib * 1024 ** 3:
        parser.error(f"{MIRROR} requires {minimum_gib} GiB of free space; use --mirror-path on a drive with room")
    lock = MIRROR / "Temp/UnityLockfile"
    if lock.exists():
        # A compiler-error exit can leave this marker behind. Do not delete it
        # or infer a live editor from its existence: acquire a read-only,
        # exclusive handle, then let Unity perform its own startup checks.
        api = ctypes.WinDLL("kernel32", use_last_error=True)
        api.CreateFileW.argtypes = [wintypes.LPCWSTR, wintypes.DWORD, wintypes.DWORD,
                                   ctypes.c_void_p, wintypes.DWORD, wintypes.DWORD, wintypes.HANDLE]
        api.CreateFileW.restype = wintypes.HANDLE
        api.CloseHandle.argtypes = [wintypes.HANDLE]
        handle = api.CreateFileW(str(lock), 0x80000000, 0, None, 3, 0x80, None)
        if handle == ctypes.c_void_p(-1).value:
            raise RuntimeError("Cannot exclusively read the background editor lock; Windows error " + str(ctypes.get_last_error()))
        api.CloseHandle(handle)
        print("Existing lock marker is unowned; Unity will validate it during normal startup", flush=True)
    started = time.time()
    files = source_files()
    if args.mirror_path is not None and any(len(str(MIRROR / f.relative_to(PROJECT))) >= 260 for f in files):
        parser.error("--mirror-path is too long for the recovered asset paths; use a shorter directory")
    print(f"Preparing {len(files)} source files; diagnostic_only={args.diagnostic_only}", flush=True)
    original_hash = None if args.diagnostic_only else fingerprint(files)
    # Seed once, then let this editor own its independent incremental cache.
    copies = list(files)
    if not cache_ready.exists():
        cache_dirs = ("Artifacts", "PackageCache", "ScriptAssemblies", "ShaderCache", "BurstCache")
        cache_files = ("ArtifactDB", "SourceAssetDB", "LibraryFormatVersion.txt", "ScriptMapper",
                       "MonoManager.asset", "BuildSettings.asset", "EditorUserBuildSettings.asset",
                       "SpriteAtlasDatabase.asset")
        # PID/IPC files, database locks, EditorInstance and Bee process state
        # belong to the source editor; never seed them into another editor.
        copies += [f for root in cache_dirs for f in (PROJECT / "Library" / root).rglob("*")
                   if f.is_file() and not f.name.endswith((".lock", "-lock", ".pid"))]
        copies += [PROJECT / "Library" / name for name in cache_files
                   if (PROJECT / "Library" / name).is_file()]
    print(f"Preparing isolated project: {len(copies)} files", flush=True)
    with ThreadPoolExecutor(max_workers=4) as pool:
        copied = sum(pool.map(sync_one, copies))
    cache_ready.touch()
    # Never silently run stale source after a file was intentionally removed.
    expected = {f.relative_to(PROJECT).as_posix() for f in files}
    stale = [f.relative_to(MIRROR).as_posix() for root in ("Assets", "Packages", "ProjectSettings")
             for f in (MIRROR / root).rglob("*") if f.is_file() and f.relative_to(MIRROR).as_posix() not in expected]
    if stale:
        raise RuntimeError("Isolated project contains removed source files; reconcile explicitly: " + repr(stale[:20]))
    print(f"Copied {copied / (1024 ** 3):.2f} GiB; launching hidden batch editor", flush=True)
    if args.test not in ("core-ui-author", "core-no-moves-author", "core-modal-tutorial-author", "core-directed-tutorial-author"):
        # Explicit test transport: isolate only the configured core progress
        # file name, preserving the production SQLite path/schema/lifecycle.
        progress_settings = MIRROR / "Assets/Foundation/Settings/CoreFlowSettings.asset"
        raw = progress_settings.read_text(encoding="utf-8")
        assert raw.count("  progressFileName: CoreLevels\n") == 1
        progress_settings.write_text(raw.replace("  progressFileName: CoreLevels\n", f"  progressFileName: CoreLevels_verify_{time.time_ns()}\n"), encoding="utf-8", newline="\n")
    for name in (REPORTS[args.test], "background_verification.json"):
        path = MIRROR / name
        if path.exists():
            path.unlink()
    log = MIRROR / ("batch_" + args.test + ".log")
    command = [str(UNITY), "-batchmode", "-projectPath", str(MIRROR),
               "-buildTarget", "Android", "-job-worker-count", "2", "-logFile", str(log),
               "-executeMethod", "Whitebox.Foundation.Editor.BackgroundVerification.Run",
               "-recoveryVerification", args.test]
    if args.opening_levels:
        command += ["-recoveryOpeningLevels", args.opening_levels]
    if args.resume_level is not None or opening_checkpoint:
        command += ["-recoveryResumeLevel", str(args.resume_level or 1)]
    process_timeout = 900
    if args.timeout_seconds is not None:
        command += ["-recoveryTimeoutSeconds", str(args.timeout_seconds)]
        process_timeout = args.timeout_seconds + 100
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = subprocess.SW_HIDE
    completed = subprocess.run(command, startupinfo=startup,
                               creationflags=subprocess.CREATE_NO_WINDOW | subprocess.BELOW_NORMAL_PRIORITY_CLASS,
                               timeout=process_timeout)
    report_path = MIRROR / "background_verification.json"
    opening_write = None
    if opening_checkpoint and completed.returncode == 0:
        report = MIRROR / REPORTS[args.test]
        opening_write = json.loads(report.read_text(encoding="utf-8-sig"))
        assert opening_write["passed"] and not opening_write["reopened_in_new_process"]
        report.unlink()
        report_path.unlink()
        read_log = MIRROR / "batch_core-opening-checkpoint_read.log"
        command[command.index("-logFile") + 1] = str(read_log)
        completed = subprocess.run(command + ["-recoveryOpeningCheckpointRead"], startupinfo=startup,
                                   creationflags=subprocess.CREATE_NO_WINDOW | subprocess.BELOW_NORMAL_PRIORITY_CLASS,
                                   timeout=process_timeout)
    unchanged = None if args.diagnostic_only else source_files() == files and fingerprint(files) == original_hash
    batch_report = json.loads(report_path.read_text(encoding="utf-8-sig")) if report_path.exists() else {}
    same_opening_profile = True
    if opening_write is not None:
        report = MIRROR / REPORTS[args.test]
        opening_read = json.loads(report.read_text(encoding="utf-8-sig")) if report.exists() else {}
        opening_keys = ("level", "save_file", "level_try_count", "difficulty_seed", "difficulty_draws")
        same_opening_profile = opening_read.get("reopened_in_new_process") is True and all(
            opening_read.get(key) == opening_write[key] for key in opening_keys
        )
    result = {
        "passed": completed.returncode == 0 and batch_report.get("passed") is True and same_opening_profile and
                  (args.diagnostic_only or unchanged),
        "canonical_source_proof": not args.diagnostic_only,
        "test": args.test, "unity_exit_code": completed.returncode,
        "source_unchanged_during_run": unchanged, "source_sha256": original_hash,
        "isolated_project": str(MIRROR), "log_path": str(log),
        "elapsed_seconds": round(time.time() - started, 2), "batch": batch_report,
        "scope": "Independent source/cache copy; production scene/runtime code. Diagnostic mode skips source hashing and supplies no canonical source proof. Each verifier defines its actual stimulus and ownership scope; a passing core-flow verifies the production win/fail/dialog owners, not natural full-level solves, all100 gameplay, physical input or original rendered parity.",
    }
    if opening_write is not None:
        result["opening_write"] = opening_write
        result["read_log_path"] = str(read_log)
    output = args.summary_path or PROJECT / (("diagnostic_" if args.diagnostic_only else "background_") + args.test.replace("-", "_") + "_verification.json")
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    if not args.diagnostic_only and unchanged and (MIRROR / REPORTS[args.test]).exists():
        shutil.copy2(MIRROR / REPORTS[args.test], PROJECT / REPORTS[args.test])
    if args.diagnostic_only:
        print(json.dumps(result, indent=2), flush=True)
        raise SystemExit(0 if result["passed"] else 1)
    if result["passed"] and args.test in ("player-input", "initial-population", "possible-moves", "generated-counts", "game-scene", "camera-projection", "life-helper", "core-catalog"):
        shutil.copy2(MIRROR / "live_player_board_observation.json", PROJECT / "live_player_board_observation.json")
    if result["passed"] and args.test in ("initial-population", "possible-moves", "generated-counts", "life-helper"):
        shutil.copy2(MIRROR / "live_game_start_verification.json", PROJECT / "live_game_start_verification.json")
    if result["passed"] and args.test == "generated-counts":
        shutil.copy2(MIRROR / "live_generated_counts_verification.json", PROJECT / "live_generated_counts_verification.json")
    if result["passed"] and args.test in ("game-scene", "camera-projection", "life-helper", "core-catalog", "core-flow"):
        shutil.copy2(MIRROR / "live_game_scene_verification.json", PROJECT / "live_game_scene_verification.json")
        shutil.copy2(MIRROR / "game_scene_initial.png", PROJECT / "game_scene_initial.png")
    if result["passed"] and args.test == "camera-projection":
        shutil.copy2(MIRROR / "live_camera_projection_verification.json", PROJECT / "live_camera_projection_verification.json")
    if result["passed"] and args.test == "core-catalog":
        shutil.copy2(MIRROR / "live_core_catalog_verification.json", PROJECT / "live_core_catalog_verification.json")
    if result["passed"] and args.test == "core-flow":
        shutil.copy2(MIRROR / "live_core_catalog_verification.json", PROJECT / "live_core_catalog_verification.json")
        for name in ("core_flow_win.png", "core_flow_level2.png", "core_flow_fail.png", "core_flow_retry.png", "core_flow_level3.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-grass":
        for name in ("grass_initial.png", "grass_after_match.png", "grass_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-grass-chain":
        for name in ("core_grass_level5.png", "core_grass_level5_refill.png", "core_grass_level6.png",
                     "core_grass_level6_light.png", "core_grass_level7.png", "core_grass_level7_refill.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test in ("core-cupboard", "core-early-chapter"):
        for name in ("core_cupboard_initial.png", "core_cupboard_door.png",
                     "core_cupboard_plates.png", "core_cupboard_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-early-chapter":
        for level in (8, 9, 10):
            name = f"core_chapter_level{level}.png"
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-random-chapter":
        for name in ("core_random_level15_opening.png", "core_random_level15_refill.png", "core_random_level19_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-coin":
        for name in ("core_coin_level20_start.png", "core_coin_level20_flight.png", "core_coin_level20_refill.png", "core_coin_level20_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-mail":
        for name in ("core_mail_level21_start.png", "core_mail_level21_flight.png", "core_mail_level21_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-mixed-chapter":
        for name in ("core_mixed_level25_start.png", "core_mixed_level28_start.png", "core_mixed_level28_combo.png",
                     "core_mixed_level29_start.png", "core_mixed_level30_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-egg-chapter":
        for name in ("core_egg_level31_start.png", "core_egg_level31_break.png", "core_egg_level38_start.png",
                     "core_egg_level40_start.png", "core_egg_level40_combo.png", "core_egg_level40_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-potion-chapter":
        for name in ("core_potion_level41_start.png", "core_potion_level41_bottle.png", "core_potion_level45_start.png",
                     "core_potion_level46_refill.png", "core_potion_level50_start.png", "core_potion_level50_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-vase-chapter":
        for name in ("core_vase_level51_start.png", "core_vase_level51_crack.png", "core_vase_level51_break.png",
                     "core_vase_level55_start.png", "core_vase_level59_start.png", "core_vase_level60_start.png",
                     "core_vase_level60_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-owl-chapter":
        for name in ("core_owl_level61_start.png", "core_owl_level61_rocket.png", "core_owl_level64_cupboard.png", "core_owl_level65_start.png",
                     "core_owl_level66_refill.png", "core_owl_level68_start.png", "core_owl_level70_start.png",
                     "core_owl_level70_refill.png", "core_owl_level70_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "core-bush-chapter":
        for name in ("core_bush_level71_start.png", "core_bush_level71_damage.png", "core_bush_level71_spread.png",
                     "core_bush_level71_grass.png", "core_bush_level74_start.png", "core_bush_level78_start.png",
                     "core_bush_level80_start.png", "core_bush_level80_shuffle.png", "core_bush_level80_win.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "life-helper":
        shutil.copy2(MIRROR / "live_life_helper_verification.json", PROJECT / "live_life_helper_verification.json")
    if result["passed"] and args.test == "life-clock-win":
        shutil.copy2(MIRROR / "live_life_clock_verification.json", PROJECT / "live_life_clock_verification.json")
    if result["passed"] and args.test == "possible-moves":
        shutil.copy2(MIRROR / "live_possible_moves_verification.json", PROJECT / "live_possible_moves_verification.json")
    if result["passed"] and args.test == "tnt-propeller-combo":
        for name in ("tnt_propeller_carrying.png", "tnt_propeller_refilled.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "ball-ball-combo":
        for name in ("ball_ball_merging.png", "ball_ball_wave.png", "ball_ball_refilled.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "propeller-rocket-combo":
        for name in ("propeller_rocket_carrying.png", "propeller_rocket_impact.png", "propeller_rocket_refilled.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test in ("remaining-coins-normal", "remaining-coins-skip", "remaining-coins-early-skip", "life-clock-win"):
        variant = "normal" if args.test == "life-clock-win" else args.test.removeprefix("remaining-coins-").replace("-", "_")
        for suffix in ("flying", "reward", "after_continue"):
            name = f"remaining_coins_{variant}_{suffix}.png"
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "goal-ui":
        for name in ("goal_ui_before_complete.png", "goal_ui_completed.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "win-logo-visual":
        shutil.copy2(MIRROR / "win_logo_visual.png", PROJECT / "win_logo_visual.png")
    if result["passed"] and args.test == "win-logo-skip":
        for name in ("win_logo_skip_before.png", "win_logo_skip_reward.png", "win_logo_skip_after_continue.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "lightball-tnt-combo":
        for name in ("lightball_tnt_converted.png", "lightball_tnt_refilled.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "lightball-rocket-combo":
        for name in ("lightball_rocket_converted.png", "lightball_rocket_refilled.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "lightball-combos":
        for name in ("live_lightball_tnt_combo_verification.json", "live_lightball_rocket_combo_verification.json",
                     "lightball_tnt_converted.png", "lightball_tnt_refilled.png",
                     "lightball_rocket_converted.png", "lightball_rocket_refilled.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "lightball-propeller-combo":
        for name in ("lightball_propeller_converted.png", "lightball_propeller_refilled.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "lightball-combo-regression":
        for family in ("propeller", "tnt", "rocket"):
            for name in (f"live_lightball_{family}_combo_verification.json", f"lightball_{family}_converted.png", f"lightball_{family}_refilled.png"):
                shutil.copy2(MIRROR / name, PROJECT / name)
    if result["passed"] and args.test == "remaining-moves-replacement":
        for name in ("remaining_moves_converted.png", "remaining_moves_reward.png"):
            shutil.copy2(MIRROR / name, PROJECT / name)
    print(json.dumps(result, indent=2), flush=True)
    raise SystemExit(0 if result["passed"] else 1)


if __name__ == "__main__":
    main()
