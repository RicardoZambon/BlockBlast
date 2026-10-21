# Phase 4a: Automated Unity Tests and Builds (4.7, First Part)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The parts of master-plan task 4.7 that need nothing but a Unity licence:
- Unity's tests on every pull request.
- An Android App Bundle and an exported Xcode project on every push to `main`.

All of it goes through GameCI in GitHub Actions, and it's skipped until you add the licence secrets.

**Spec:** [master plan](2026-10-15-block-puzzle-game.md), 4.6, 4.7 and the Global Constraints' secrets rule.

## Rules

- **Nothing breaks without the secrets:**
  - A first job checks for `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD`.
  - Without all three, every Unity job is skipped with a notice, and only the engine tests run.
  - The `test` check that `main` requires stays the engine one, so the new jobs never block a merge.
- **When things run:**
  - The tests run on pull requests and on `main`.
  - The builds run on `main` and on demand, since they take a long time and a pull request only needs the tests.
- **Versions:** 1.0.0, and the Android version code is the workflow's run number, so it always goes up (4.6).
- **Signing:** with the four Android keystore secrets the bundle is signed with your upload key; without them, Unity's debug key signs it.
- **Secrets:** never committed. The README lists each one and where it comes from.
- **Pinned actions:**
  - `game-ci/unity-builder@v4` (v4.8.2) and `game-ci/unity-test-runner@v4` (v4.3.2).
  - Not unity-builder v6: it's six weeks old and downloads a "latest" command-line tool every run.
  - GameCI publishes images for Unity 6000.3.26f1: Ubuntu base, Android and iOS.

## Tasks

- [x] `.github/workflows/unity.yml`: the licence check, the tests, the Android bundle and the Xcode export, each with a Library cache and its results uploaded as artifacts.
- [x] The README's "Automated builds" section: what runs, and the secrets to add.

## Results

Written on 2026-10-21.
- **Checked against GameCI's v4 sources:** a non-empty keystore name turns custom signing on (`AndroidSettings.cs`), so the workflow passes the name only when the keystore secret exists. Otherwise a build without it would fail looking for the file.
- **On the pull request:** the Unity jobs skipped as intended, since the secrets didn't exist yet. The licence check ran, which proved GitHub accepts the workflow file.
- **First full run:** you added the secrets on 2026-10-21, and Claude ran the workflow on `main` ([run 39592017893](https://github.com/RicardoZambon/BlockBlast/actions/runs/39592017893)). Everything passed, with no Library cache yet:
  - the licence check, in 3 s;
  - Unity's tests: all 280 (193 EditMode, 87 PlayMode), in 5 minutes;
  - the Android App Bundle, signed with Unity's debug key until the keystore secrets exist: a 33 MB artifact, in 25 minutes;
  - the Xcode project: a 235 MB artifact, in 8 minutes.
- **If you later require the Unity tests on `main`:** require the `tests` job's check. The test runner's own "Unity tests" check reports a "neutral" result even when every test passes.
- **Still to do for 4.7, with your accounts:**
  - The macOS job that archives the iOS build and uploads it to TestFlight with fastlane, using an App Store Connect API key and your signing certificates.
  - fastlane `supply` for Play's internal track, using a service-account key.
