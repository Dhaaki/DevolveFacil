# La Moda Design System — Candy Theme

A bold, modern design language built on Tailwind CSS v4. High contrast, rounded surfaces, vibrant primary colors, and micro-interactions that make the UI feel alive.

---

## Stack

| Concern | Tool |
|---|---|
| Styling | Tailwind CSS v4 (`@theme` config in CSS) |
| Components | Custom React components |
| Animations | Framer Motion (`motion/react`) + CSS keyframes |
| Icons | Lucide React |
| Class merging | `clsx` + `tailwind-merge` via `cn()` |
| Fonts | DM Sans (body), Outfit (display) — Google Fonts |

---

## Color Tokens

Tokens are CSS custom properties registered in `index.css` and exposed as Tailwind utilities via `@theme`.

### Light Mode

| Token | Utility | Hex | Usage |
|---|---|---|---|
| `--candy-bg` | `bg-candy-bg` | `#fffbf9` | Page background |
| `--candy-surface` | `bg-candy-surface` | `#ffffff` | Cards, sidebars, headers |
| `--candy-surface-variant` | `bg-candy-surface-variant` | `#fcf4f2` | Input backgrounds, subtle fills |
| `--candy-primary` | `bg-candy-primary` / `text-candy-primary` | `#f43f5e` | CTAs, active states, accents |
| `--candy-primary-container` | `bg-candy-primary-container` | `#fff1f2` | Tinted card backgrounds |
| `--candy-secondary` | `bg-candy-secondary` / `text-candy-secondary` | `#f97316` | Secondary actions, highlights |
| `--candy-tertiary` | `bg-candy-tertiary` / `text-candy-tertiary` | `#8b5cf6` | Charts, decorative elements |
| `--candy-on-surface` | `text-candy-on-surface` | `#1e1b4b` | Primary text |
| `--candy-on-surface-variant` | `text-candy-on-surface-variant` | `#64748b` | Secondary text, placeholders, labels |

### Dark Mode

Applied via `.dark` class on `<html>`. ThemeProvider handles toggling and persists to `localStorage`.

| Token | Dark Value |
|---|---|
| `--candy-bg` | `#0b0e14` |
| `--candy-surface` | `#131720` |
| `--candy-surface-variant` | `#1c2230` |
| `--candy-on-surface` | `#f1f5f9` |
| `--candy-on-surface-variant` | `#94a3b8` |

Primary, secondary, and tertiary colors are shared between modes.

### Semantic Colors (non-token)

Used directly with Tailwind utilities:

| Meaning | Text | Background |
|---|---|---|
| Success | `text-green-500` | `bg-green-500/10` |
| Warning | `text-orange-500` | `bg-orange-500/10` |
| Error | `text-red-500` | `bg-red-500/10` |

---

## Typography

```css
--font-sans: "DM Sans", ui-sans-serif, system-ui, sans-serif;   /* body */
--font-display: "Outfit", sans-serif;                            /* headings */
```

Body uses `font-sans` by default (set on `<body>`).

### Scale in use

| Element | Classes |
|---|---|
| Page title | `text-4xl font-bold tracking-tight text-candy-on-surface` |
| Section heading | `text-xl font-bold text-candy-on-surface` |
| Card subheading | `text-sm font-bold text-candy-on-surface-variant uppercase tracking-wider` |
| Body / description | `text-sm font-medium text-candy-on-surface-variant` |
| Micro label | `text-[10px] font-bold uppercase tracking-widest` |
| Badge / tag | `text-[11px] font-bold` |

---

## Spacing & Layout

### App Shell

```
┌─────────────────────────────────────────────┐
│  Sidebar (280px, sticky)  │  Main content   │
│                           │  Header (100px) │
│                           │  Page content   │
└─────────────────────────────────────────────┘
```

- Sidebar: `w-[280px]`, `sticky top-0 min-h-screen`
- Header: `h-[100px]`, `sticky top-0 z-40`, `backdrop-blur-xl`
- Page padding: `p-10` on the content wrapper
- Max content width: `max-w-[1600px] mx-auto`

### Grid Patterns

```tsx
// Stats row (3 col)
<div className="grid grid-cols-1 md:grid-cols-3 gap-8">

// Main + sidebar panel (2/3 + 1/3)
<div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
  <Card className="lg:col-span-2"> ... </Card>
  <Card> ... </Card>
</div>

// Two equal columns
<div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
```

---

## Border Radius

| Token | Value | Utility |
|---|---|---|
| `--radius-candy-card` | `24px` | `rounded-[24px]` or `rounded-3xl` |
| `--radius-candy-pill` | `9999px` | `rounded-full` |
| Button (medium) | `16px` | `rounded-2xl` |
| Button (large CTA) | `9999px` | `rounded-full` |
| Icon container | `16px` | `rounded-2xl` |
| Input | `24px` | `rounded-[24px]` |

---

## Shadows

```css
/* Card default */
shadow-[0_10px_40px_-10px_rgba(124,58,237,0.15)]

/* Primary glow (buttons, badges) */
shadow-xl shadow-candy-primary/20

/* Secondary glow */
shadow-lg shadow-candy-secondary/20

/* Utility classes */
.candy-shadow    → 0 10px 40px -10px rgba(244, 63, 94, 0.15)
.candy-shadow-sm → 0 4px  20px -5px  rgba(244, 63, 94, 0.10)
```

---

## Animations

### CSS utilities

| Class | Behavior |
|---|---|
| `.hover-bounce` | Lifts `-translate-y-1 scale-[1.02]` on hover, 300ms transition |
| `.active-shrink` | Scales to `scale-95` on `:active` |
| `animate-bounce-subtle` | Gentle float loop: `translateY(0 → -4px)`, 2s infinite |
| `animate-spin-slow` | Full rotation, 8s linear infinite |
| `animate-pulse` | Tailwind default — used on live indicators |

### Framer Motion

Cards use `motion.div`. Standard entry pattern:

```tsx
import { motion } from 'motion/react';

<motion.div
  initial={{ opacity: 0, y: 20 }}
  animate={{ opacity: 1, y: 0 }}
>
  ...
</motion.div>
```

---

## Components

### `Card`

```tsx
import { Card } from '@/src/components/ui/Card';

// Variants
<Card variant="white">...</Card>   // default — white surface, subtle border
<Card variant="glass">...</Card>   // frosted glass, backdrop-blur
<Card variant="tinted">...</Card>  // primary-tinted background
```

All cards: `rounded-[24px]`, `p-6`, `border`, primary-glow shadow. Built on `motion.div` — accepts all Framer Motion props.

### `Badge`

```tsx
import { Badge } from '@/src/components/ui/Badge';

<Badge variant="primary">Live</Badge>    // rose fill, white text
<Badge variant="secondary">New</Badge>   // orange fill, white text
<Badge variant="tertiary">Draft</Badge>  // surface-variant fill
<Badge variant="success">Healthy</Badge> // green tint
<Badge variant="warning">Low</Badge>     // orange tint
<Badge variant="error">Out</Badge>       // red tint
<Badge variant="outline">Tag</Badge>     // transparent, border only
```

All badges: `rounded-full`, `px-3 py-1`, `text-[11px] font-bold`.

---

## Interactive Patterns

### Icon button (header/toolbar)

```tsx
<button className="w-12 h-12 flex items-center justify-center text-candy-on-surface-variant hover:bg-candy-primary/10 hover:text-candy-primary rounded-2xl transition-all">
  <Bell size={24} />
</button>
```

### Primary CTA button

```tsx
<button className="h-14 px-8 bg-candy-primary text-white font-bold rounded-full hover-bounce shadow-xl shadow-candy-primary/20">
  New Order
</button>
```

### Secondary action button

```tsx
<button className="h-14 px-6 bg-candy-secondary/10 text-candy-secondary rounded-2xl font-bold hover-bounce transition-all">
  Connect Channel
</button>
```

### Form input

```tsx
<input className="w-full h-14 pl-14 pr-6 bg-candy-surface-variant border-transparent rounded-[24px] outline-none focus:bg-candy-surface focus:ring-4 focus:ring-candy-primary/10 transition-all font-medium text-sm text-candy-on-surface placeholder:text-candy-on-surface-variant/50" />
```

### Navigation link (active / inactive)

```tsx
// active
"bg-candy-primary text-white shadow-xl shadow-candy-primary/20 scale-105"

// inactive
"text-candy-on-surface-variant hover:bg-candy-primary/5 hover:text-candy-primary"

// shared
"flex items-center gap-4 px-6 py-4 rounded-2xl transition-all font-bold text-sm"
```

### Avatar / initials

```tsx
<div className="w-12 h-12 rounded-2xl bg-gradient-to-br from-candy-primary to-candy-secondary flex items-center justify-center text-white font-bold shadow-lg">
  AS
</div>
```

### Live indicator dot

```tsx
<span className="w-2 h-2 rounded-full bg-candy-primary animate-pulse" />
```

---

## Utility: `cn()`

Merges Tailwind classes correctly — handles conflicts and conditional classes.

```tsx
import { cn } from '@/src/lib/utils';

<div className={cn('base-class', condition && 'conditional-class', className)} />
```

---

## ThemeProvider

Wrap the app root. Reads from `localStorage` and `prefers-color-scheme` on init.

```tsx
import { ThemeProvider, useTheme } from '@/src/lib/ThemeProvider';

// In App root:
<ThemeProvider>
  <App />
</ThemeProvider>

// In any component:
const { theme, toggleTheme } = useTheme();
```

---

## CSS Setup (new project checklist)

```css
/* index.css */
@import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;700&family=Outfit:wght@400;600;700&display=swap');
@import "tailwindcss";

:root {
  --candy-bg: #fffbf9;
  --candy-surface: #ffffff;
  --candy-surface-variant: #fcf4f2;
  --candy-primary: #f43f5e;
  --candy-on-surface: #1e1b4b;
  --candy-on-surface-variant: #64748b;
  --candy-secondary: #f97316;
  --candy-tertiary: #8b5cf6;
}

.dark {
  --candy-bg: #0b0e14;
  --candy-surface: #131720;
  --candy-surface-variant: #1c2230;
  --candy-on-surface: #f1f5f9;
  --candy-on-surface-variant: #94a3b8;
}

@theme {
  --font-sans: "DM Sans", ui-sans-serif, system-ui, sans-serif;
  --font-display: "Outfit", sans-serif;

  --color-candy-bg: var(--candy-bg);
  --color-candy-surface: var(--candy-surface);
  --color-candy-surface-variant: var(--candy-surface-variant);
  --color-candy-primary: var(--candy-primary);
  --color-candy-primary-container: #fff1f2;
  --color-candy-secondary: var(--candy-secondary);
  --color-candy-tertiary: var(--candy-tertiary);
  --color-candy-on-surface: var(--candy-on-surface);
  --color-candy-on-surface-variant: var(--candy-on-surface-variant);

  --radius-candy-card: 24px;
  --radius-candy-pill: 9999px;

  --animate-bounce-subtle: bounce-subtle 2s infinite ease-in-out;
  --animate-spin-slow: spin 8s linear infinite;

  @keyframes bounce-subtle {
    0%, 100% { transform: translateY(0); }
    50% { transform: translateY(-4px); }
  }
}

@layer base {
  body {
    @apply bg-candy-bg text-candy-on-surface font-sans antialiased;
  }
}

.hover-bounce { @apply transition-all duration-300; }
.hover-bounce:hover { @apply -translate-y-1 scale-[1.02]; }
.active-shrink { @apply active:scale-95 transition-transform; }
```

### Required packages

```bash
npm install react react-dom react-router-dom motion lucide-react clsx tailwind-merge recharts
npm install -D tailwindcss @tailwindcss/vite @vitejs/plugin-react typescript
```
