# SceneKeeper

***SceneKeeper*** is a lightweight, SFW roleplay scene assistant for FFXIV.

It is designed to help roleplayers keep active scenes organized, readable, and easy to revisit. Whether you are writing in a busy venue, hosting a companion session, running a group scene, or tracking a long-term story arc, SceneKeeper gives you a cleaner space to manage the moment.

---

## ✦ Features

### ***Scene Partner Tracking***

Keep track of the characters currently involved in your active RP scene.

- Add and remove scene partners
- Maintain a focused roster for the current scene
- See which tracked partners are nearby
- Add partner notes
- Add relationship or context details
- Add partner tags for easier organization
- Assign custom partner colors for easier visual tracking

---

### ***Partner Colors***

SceneKeeper lets you assign custom colors to tracked partners.

This is useful when you are writing with multiple people and want to quickly tell who is who at a glance.

Partner colors can be used for:

- Nearby partner markers
- Soft visual highlights
- Multi-person scenes
- Venue scenes
- Group RP
- Long-running story groups

---

### ***Captured Chat***

SceneKeeper provides a cleaner local view of RP-relevant chat.

- Capture selected chat types
- Review recent scene messages
- Select individual lines
- Pin important lines
- Copy selected scene context
- Copy selected chat as Markdown
- Clear captured chat between scenes

---

### ***Chat Type Toggles***

Choose which chat types SceneKeeper should include in the captured chat log.

This helps keep the log focused and avoids unnecessary clutter.

Supported toggles may include:

- Say
- Emote
- Yell
- Shout
- Tell
- Party
- Alliance
- Free Company
- Linkshell
- Cross-world Linkshell

---

### ***Partner Chat Filter***

Busy RP spaces can get messy fast. SceneKeeper includes captured chat filtering so you can focus on the people actually involved in your scene.

Filter modes may include:

- All Captured Chat
- Scene Partners Only
- Scene Partners + Me
- Pinned Only

This is especially useful for:

- Multi-scene venues
- Crowded RP rooms
- Group RP
- Companion sessions
- Events with overlapping conversations

SceneKeeper can also show how many captured lines are visible after filtering, making it easier to see when the view is narrowed.

---

### ***Captured Chat Search***

Search through captured chat by:

- Sender name
- Message content
- Chat type

This makes it easier to find a specific line, moment, or speaker without scrolling through everything manually.

---

### ***Add Sender as Partner***

When reviewing captured chat, SceneKeeper can help you quickly add a sender as a scene partner.

This is useful when someone joins the scene after it has already started or when you want to quickly build your partner list from active chat.

---

### ***Pinned Scene Lines***

Pin important lines from captured chat so they are easy to find later.

Pinned lines are useful for:

- Memorable dialogue
- Emotional beats
- Important lore reveals
- Promises or threats
- Scene quotes
- Journal excerpts
- Recap writing

Pinned lines are saved with scene history.

---

### ***Scene Notes***

Use the notes area to keep track of scene details while you write.

Examples:

- Current scene premise
- Important names
- Locations
- Character intentions
- OOC reminders
- Scene tone
- Hooks to revisit later
- Plot points to remember

---

### ***Follow-Up Tasks***

Add simple follow-up tasks to a scene so important next steps are not forgotten.

Examples:

- Update character journal
- Send a follow-up message
- Schedule the next scene
- Add a new character note
- Save screenshots
- Write a recap
- Follow up on a plot hook

Follow-up tasks are saved with scene history.

---

### ***Scene Tags***

Add tags to scenes for easier organization.

Examples:

- Romance
- Mystery
- Combat
- Venue
- Important
- Follow-up
- Completed
- Ongoing
- Character Arc

Tags are included in saved scene history and Markdown exports.

---

### ***Nearby Partner Detection***

SceneKeeper can show whether your tracked scene partners are currently nearby.

This is especially helpful for:

- Busy RP venues
- Multi-room events
- Companion sessions
- Group scenes
- Large social gatherings

---

### ***Soft Visual Markers***

Optional soft markers can help identify tracked scene partners on screen.

These are meant to be subtle, readable, and non-intrusive.

Marker options may include:

- Marker visibility
- Marker size
- Highlight size
- Global marker color
- Partner-specific marker colors
- Partner visibility state

---

### ***Scene History***

Save completed or important scenes to History so they can be revisited later.

Saved scenes may include:

- Scene name
- Scene notes
- Scene tags
- Scene partners
- Partner notes
- Partner relationship/context details
- Partner tags
- Captured chat
- Pinned lines
- Follow-up tasks

History can be searched, copied, loaded, or deleted.

---

### ***Searchable History***

Search your saved scenes by details such as:

- Scene name
- Partner name
- Tags
- Notes
- Captured dialogue
- Pinned lines

This makes SceneKeeper useful for long-term RP continuity and recurring character arcs.

---

### ***Markdown Export***

SceneKeeper can copy scene information in Markdown format for easy use outside the game.

Useful for:

- Discord recaps
- Character journals
- Carrd pages
- GitHub notes
- Personal archives
- Story summaries
- RP continuity tracking

You can copy:

- Current scene as Markdown
- Selected captured chat as Markdown
- Saved scenes as Markdown
- Scene summaries
- Pinned lines

---

### ***Scene Builder***

Scene Builder gives long-form RP writers a dedicated space to draft up to five RP paragraphs before posting.

Each paragraph box includes a character counter so you can keep your writing inside the configured chat limit.

Scene Builder includes:

- Five paragraph boxes
- Paragraph labels from 1/5 through 5/5
- Character limit counter
- Configurable paragraph limit
- Guided clipboard queue
- Copy Next button
- Copy All button
- Clear Builder button
- Panic Stop button

Scene Builder is especially useful for players who normally write RP posts in Notepad, Discord, or another external editor before copying them into chat.

---

### ***Guided Queue***

The Guided Queue helps you post multi-part RP responses safely.

Instead of automatically sending messages, SceneKeeper copies each paragraph to your clipboard one at a time. You stay in control of when each paragraph is pasted and sent.

Basic flow:

```text
Write paragraphs.
Start Guided Queue.
Paste and send paragraph 1 in chat.
Click Copy Next.
Paste and send paragraph 2.
Repeat until finished.
```

The queue can be stopped at any time with ***Panic Stop***.

---

### ***Button Feedback***

SceneKeeper provides visible feedback when actions are completed.

Examples:

- Copied!
- Scene saved.
- Scene loaded.
- Markdown copied.
- Summary copied.
- Builder cleared.
- Queue stopped.

This helps make it clear when a button has successfully done something.

---

## ✦ Commands

```text
/sk
/scenekeeper

/sk add <name>
/sk remove <name>
/sk clear
/sk scene <scene name>
/sk start
/sk pause
/sk resume
/sk save
/sk new
/sk settings
```

---

## ✦ Installation

SceneKeeper is distributed through a custom Dalamud repository.

1. Open Dalamud settings in-game.
2. Go to the Experimental tab.
3. Add the custom repository URL.
4. Save and close settings.
5. Open the plugin installer.
6. Search for ***SceneKeeper***.
7. Install and enable the plugin.

Repository URL:

```text
https://raw.githubusercontent.com/rubyblaire/scenekeeper/main/pluginmaster.json
```

---

## ✦ Requirements

- FFXIV
- Dalamud
- Custom plugin repositories enabled
- Compatible with Dalamud API 15

---

## ✦ Purpose

SceneKeeper is made for ***organization, comfort, and continuity*** during roleplay.

It does not replace roleplay, automate roleplay, or control your character.

It simply gives you a cleaner space to keep track of:

- Who is in your scene
- What has been said
- Which lines matter most
- Who is nearby
- What needs to happen next
- Which scenes are worth saving
- What details you want to remember
- Which paragraphs you are preparing to post

The goal is to make RP smoother, especially when chat moves quickly, a scene has multiple participants, or a writer wants to prepare longer responses in advance.

---

## ✦ Intended Use

SceneKeeper is designed for ***SFW roleplay support***.

It is useful for:

- Venue roleplay
- Companion or host sessions
- Story-driven scenes
- Group scenes
- Long-form RP
- Character meetings
- Event nights
- Personal scene organization
- Continuity tracking
- Scene recaps
- RP journaling
- Multi-paragraph writing

---

## ✦ Privacy

SceneKeeper is designed as a local roleplay organization tool.

- Scene notes are stored locally.
- Scene partner lists are stored locally.
- Scene history is stored locally.
- Captured chat is handled locally.
- Scene Builder text is handled locally.
- No analytics are included.
- No remote logging is included.

---

## ✦ Customization

SceneKeeper includes options for adjusting how scene information is displayed and saved.

Customization may include:

- Chat log filters
- Partner chat filtering
- Captured chat search
- Marker visibility
- Marker size
- Highlight size
- Global marker color
- Partner-specific colors
- Scene tracking behavior
- Captured chat cleanup
- Maximum saved scenes
- Scene Builder character limit
- History search behavior

---

## ✦ Development

SceneKeeper is developed by ***Ruby Blaire***.

This plugin is distributed through a custom repository and is not affiliated with or endorsed by Square Enix, Dalamud, or XIVLauncher.

---

## ✦ Disclaimer

SceneKeeper is a third-party plugin for use with Dalamud.

Use responsibly and in accordance with the rules and expectations of the communities, venues, and spaces you participate in.

---

## ✦ License

All rights reserved unless otherwise stated.
