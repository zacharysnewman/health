# [2.1.0](https://github.com/zacharysnewman/health/compare/v2.0.0...v2.1.0) (2026-03-18)


### Features

* add HealthFXAdapter for drop-on SFX and VFX prefab wiring ([1b10edf](https://github.com/zacharysnewman/health/commit/1b10edfb43522af094d29aef36d4f4407168ec29))

# [2.0.0](https://github.com/zacharysnewman/health/compare/v1.4.1...v2.0.0) (2026-03-17)


* feat!: replace Damage(float) with TakeDamage(DamageInfo) via IDamageable ([b5dcfaf](https://github.com/zacharysnewman/health/commit/b5dcfafa4dac304ee2f53aecc0a7e4683ebf4632))


### BREAKING CHANGES

* Health now implements IDamageable from combat-contracts.
The Damage(float) method is replaced by TakeDamage(DamageInfo) which
carries richer context (point, direction, instigator, source tag).
All networking adapters and samples updated accordingly.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>

## [1.4.1](https://github.com/zacharysnewman/health/compare/v1.4.0...v1.4.1) (2026-03-15)


### Bug Fixes

* add #if HEALTH_QUANTUM compiler guards to all Quantum port files ([699273d](https://github.com/zacharysnewman/health/commit/699273d82c216e526ba358e67a2ae021ad8b3cf0))

# [1.4.0](https://github.com/zacharysnewman/health/compare/v1.3.0...v1.4.0) (2026-03-15)


### Features

* add Quantum 3 port of health system ([d2b63df](https://github.com/zacharysnewman/health/commit/d2b63df42296058b8ba91c987d79268f12d40838))

# [1.3.0](https://github.com/zacharysnewman/health/compare/v1.2.0...v1.3.0) (2026-03-14)


### Bug Fixes

* fire OnDieEvent for late joiners and document client-side events ([5ce88f0](https://github.com/zacharysnewman/health/commit/5ce88f0613b398bf7ad77fcb865264071049b3bd))


### Features

* add optional networking integration adapters for Mirror, NGO, and Photon Fusion ([d4e2650](https://github.com/zacharysnewman/health/commit/d4e26501504af4b046e8fac8020ea105c0d3e2fc))
* add singleplayer and multiplayer samples ([96b3e13](https://github.com/zacharysnewman/health/commit/96b3e13d1b463124fca2586eea7e52ab8ebd94c2))
* broadcast semantic health events to all clients ([9ee3775](https://github.com/zacharysnewman/health/commit/9ee3775832756693fe2180ff8bc2ca384dfe8d18))
* implement OnMaxHealthEvent and OnMaxShieldEvent ([b5fc9bf](https://github.com/zacharysnewman/health/commit/b5fc9bf327b2762704bc22ccd135ee5df20740e2))
* replicate MaxHealthBonus across all networking adapters ([01711d4](https://github.com/zacharysnewman/health/commit/01711d4a6f72175f0425320e61c09edfddcc95ab))

# [1.2.0](https://github.com/zacharysnewman/health/compare/v1.1.0...v1.2.0) (2026-03-04)


### Features

* rewrite README with full API documentation ([132611f](https://github.com/zacharysnewman/health/commit/132611f1d2f5f5fde0c76814607c0c3e418ec0e4))

# [1.1.0](https://github.com/zacharysnewman/health/compare/v1.0.2...v1.1.0) (2026-02-25)


### Features

* add CLAUDE.md with conventional commit guidelines ([6d5d7e8](https://github.com/zacharysnewman/health/commit/6d5d7e8000dc3c3e26e78a249acc71836b1c4477))

## [1.0.2](https://github.com/zacharysnewman/health/compare/v1.0.1...v1.0.2) (2024-06-28)


### Bug Fixes

* **meta:** more conflicts with meta files ([658bee2](https://github.com/zacharysnewman/health/commit/658bee2d3392b67c6baddaf7cd8f987e4cb794d9))

## [1.0.1](https://github.com/zacharysnewman/health/compare/v1.0.0...v1.0.1) (2024-06-28)


### Bug Fixes

* **meta:** Removed samples meta file ([faebef1](https://github.com/zacharysnewman/health/commit/faebef166abd16c5120951c7d988c0735b82176c))

# 1.0.0 (2024-06-20)


### Features

* **README.md:** Update README.md ([ba64e33](https://github.com/zacharysnewman/health/commit/ba64e330d7bf328247a91e5067e65f2a8482124c))

# 1.0.0 (2022-11-26)


### Bug Fixes

* **tests:** Removed a line ([4fc905c](https://github.com/zacharysnewman/splitscreen/commit/4fc905c2df962c19ce332e3b453b3e2662812799))
