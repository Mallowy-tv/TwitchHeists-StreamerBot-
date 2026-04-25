---
name: visual-companion
description: Use the local browser server to show mockups, diagrams, and visual comparisons during brainstorming
parent: brainstorm
---

# Visual Companion

Show mockups, diagrams, and side-by-side comparisons in the browser during brainstorming. Use this for questions where seeing is better than reading.

When the user asks to **show**, **render**, **preview**, **mock up**, or **compare visually**, do not answer with prose alone if the local browser server is available. Push a screen to the browser.
When the user needs to choose between design alternatives, default to showing mockups first instead of running an extended design interview in chat.
If the visual companion assets exist, do **not** ask for permission to start the server, write the HTML fragment, or render the preview. Writes inside `.github\.mimir-visual\` are pre-approved for this workflow. Do the work immediately, then tell the user where to look.
Use a hybrid response: render the options in the browser, summarize them briefly in chat, and tell the user to return to chat and name the option they want.

## When to use (per question)

**Use the browser for:**
- UI mockups and wireframes
- Architecture diagrams and component maps
- Side-by-side design comparisons
- Layout and visual hierarchy questions
- Flowcharts and state machines
- Requests for named page or section variants such as "show me three layout options for the Features section"
- Any request where the user is selecting one or more design directions from alternatives

**Use the terminal for:**
- Requirements and scope questions
- Conceptual A/B/C choices
- Trade-off lists and pros/cons
- Technical decisions and API design
- Anything where the answer is words
- Minor design changes that can be explained clearly without a mockup

A question *about* a visual topic is not automatically a visual question. "What kind of navigation do you want?" is conceptual — terminal. "Which of these navigation layouts feels right?" is visual — browser.

If the user asks to see options, the answer belongs in the browser even if each option also needs a short caption.

## Render order

For visual requests, use this order:
1. Ensure `.github\.mimir-visual\screens\` exists.
2. Write the new fragment file first.
3. Start the server.
4. Confirm the visual companion can serve that screen.
5. Only then tell the user the URL.

Do **not** announce "server ready" before the fragment exists and the screen is available.

## Starting the server

```powershell
# From the project root on Windows
powershell -File .\.github\scripts\server\start-server.ps1 -ProjectDir .
```

The server outputs a JSON line with `url`, `screen_dir`, and `state_dir`. Save these values. They should resolve under `.github\.mimir-visual\`.

If the script does not exist or the server fails to start, fall back to text-only brainstorming and do not block the session.

Tell the user: "I've started the visual companion at http://localhost:<port> — open that in your browser." Only do this after the fragment has been written and the screen is ready.

## Writing a screen

Write an HTML content fragment (not a full document) to a new file in `screen_dir`.

The goal is a **low-fidelity layout mockup**, not a production build. Default to:
- skeleton blocks instead of real components
- placeholder labels instead of full product copy
- simple icons or icon placeholders for navigation and actions
- structural layout differences, not polished styling

Do **not** import the application's full design system or try to recreate the live UI exactly just to show alternatives.
If a card needs more room, let it span the full options row with `class="option full-width"` or force a stacked layout with `class="option stacked"` instead of cramming content into a narrow column.
Do not make the cards clickable or add in-browser submit controls for choosing an option.
Prefer the built-in mockup primitives in `ui.html` over one-off custom layout classes. Reach for shared helpers first: `mockup-grid-2`, `mockup-grid-3`, `mockup-section`, `mockup-card-group`, `mockup-rail`, `mockup-actions`, `mockup-card-stack`, `mockup-row`, and `mockup-sidebar-layout`.

Example fragment:

```html
<h2>Which layout works better?</h2>
<p class="subtitle">Consider readability and navigation flow</p>

<div class="options">
  <div class="option" data-choice="a">
    <div class="letter">A</div>
    <div class="content">
      <h3>Option Name</h3>
      <p>Description of this approach</p>
      <div class="mockup-shell">
        <div class="mockup-topbar">
          <span class="mockup-icon"></span>
          <span class="mockup-pill short"></span>
          <span class="mockup-pill"></span>
        </div>
        <div class="mockup-hero"></div>
        <div class="mockup-row">
          <div class="mockup-card"></div>
          <div class="mockup-card"></div>
        </div>
      </div>
    </div>
  </div>
  <div class="option" data-choice="b">
    <div class="letter">B</div>
    <div class="content">
      <h3>Option Name</h3>
      <p>Description of this approach</p>
      <div class="mockup-shell">
        <div class="mockup-sidebar-layout">
          <div class="mockup-sidebar"></div>
          <div class="mockup-main">
            <div class="mockup-hero compact"></div>
            <div class="mockup-card-stack">
              <div class="mockup-card"></div>
              <div class="mockup-card"></div>
              <div class="mockup-card"></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>

<p class="subtitle">Take a look and let me know what you think.</p>
```

For more editorial or asymmetrical options, prefer built-in shared primitives over ad hoc wrappers. Example:

```html
<div class="mockup-shell">
  <div class="mockup-topbar">
    <span class="mockup-icon"></span>
    <span class="mockup-pill short"></span>
    <span class="mockup-pill"></span>
  </div>

  <div class="mockup-grid-3">
    <div class="mockup-rail vertical">
      <div class="mockup-rail-card tight">
        <span class="mockup-pill short"></span>
        <span class="mockup-pill"></span>
        <span class="mockup-pill"></span>
      </div>
      <div class="mockup-rail-card tight">
        <span class="mockup-pill short"></span>
        <div class="mockup-card"></div>
      </div>
    </div>

    <div class="mockup-section">
      <span class="mockup-pill short"></span>
      <div class="mockup-hero tall"></div>
      <div class="mockup-actions">
        <span class="mockup-pill"></span>
        <span class="mockup-pill short"></span>
      </div>
    </div>

    <div class="mockup-card-stack">
      <div class="mockup-card"></div>
      <div class="mockup-section tight">
        <span class="mockup-pill short"></span>
        <div class="mockup-card"></div>
      </div>
    </div>
  </div>
</div>
```

For layout questions, each option should include a miniature render, wireframe, or strong structural mockup of the layout itself. Do not use option cards that only describe the differences in text.
Prefer skeletons, containers, spacing, and icons over polished visuals.
Avoid inventing lots of one-off class names for spacing and alignment unless the shared primitives genuinely cannot express the layout.

Use semantic filenames: `layout.html`, `navigation.html`, `colour-scheme.html`. Never reuse filenames — each screen is a new file.

## Getting the choice back

After showing the browser preview, summarize the options briefly in chat and tell the user to return to chat and state which option they want.
Do not rely on browser click capture or submit buttons for the final decision.

## Returning to terminal

When the next question doesn't need the browser, push a waiting screen:

```html
<div style="display:flex;align-items:center;justify-content:center;min-height:60vh">
  <p class="subtitle">Continuing in terminal...</p>
</div>
```

## Rules

- Max 4 options per screen
- Use real content when it matters — placeholder text hides design problems
- Always tell the user the URL with every visual question
- Never reuse filenames
- Prefer rendered previews over prose summaries inside the screen
- Prefer low-fidelity skeleton mockups over full builds or high-effort polished visuals
- Let options stack or span full width when that makes the layout easier to read
- Do not ask for approval before writing `.github\.mimir-visual\screens\` fragments for a visual request
- Pair every visual choice screen with a short text outline of the options in chat
- Tell the user to reply in chat with the option they want
- Never generate standalone `preview.html`, `render.js`, or `file://`-based outputs for visual questions when the server flow is available
- Never use npm/playwright/browser-automation scaffolding as a substitute for the visual companion server
- End the server when brainstorming is complete: `powershell -File .\.github\scripts\server\stop-server.ps1`
