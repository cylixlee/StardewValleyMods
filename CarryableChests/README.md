# Carryable Chests

Stardew Valley mod for picking up and moving chests without emptying them first.

![carryable chests](./img/cover.png)

## How To Use

- Right-click a placed chest to open it as usual.
- Left-click a chest to pick it up. By default, your hands must be empty.
- Right-click while holding a chest to open it.
- Place the chest like any other item.

## Settings

This mod provides the following settings in [GMCM (Generic Mod Config Menu)](https://www.nexusmods.com/stardewvalley/mods/5098).

### Maximum Reach

How far away a chest can be picked up from. Defaults to `1`.

### Require Empty Hands

Whether your hands must be empty before picking up a chest. Defaults to enabled.

### Open Held Chest

Whether right-click opens the chest you're carrying. Defaults to enabled.

### Heavy Chest Slowdown

Whether fuller carried chests slow you down.

#### Enabled

Whether heavy chest slowdown is enabled. Defaults to `false`.

#### Starts At

Chest fill percentage before slowdown starts. Defaults to `50`.

Set this to `100` if you only want full chests to slow you down.

#### Max Penalty

Maximum speed penalty when the carried chest is full. Defaults to `1`.

#### Show Icon

Whether to show the slowdown debuff icon in the UI. Defaults to `false`.

## Multiplayer

PC multiplayer is supported. Farmhands can pick up and place chests; the host coordinates the world changes.

Carried chests stay carried across saves, disconnects, and reconnects. They are not automatically returned to the world.

## Supported Chests

All player chest variants are supported, including normal chests, stone chests, big chests, Junimo Chests, Mini Shipping Bins, auto-loaders, and enrichers.

Non-player reward, treasure, and gift chests are not carryable.

## Limitations

Multiplayer support is currently targeted at PC. Android multiplayer is not validated yet.
