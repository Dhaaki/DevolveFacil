/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        'candy-bg':               'var(--candy-bg)',
        'candy-surface':          'var(--candy-surface)',
        'candy-surface-variant':  'var(--candy-surface-variant)',
        'candy-primary':          'var(--candy-primary)',
        'candy-primary-container':'#fff1f2',
        'candy-secondary':        'var(--candy-secondary)',
        'candy-tertiary':         'var(--candy-tertiary)',
        'candy-on-surface':       'var(--candy-on-surface)',
        'candy-on-surface-variant':'var(--candy-on-surface-variant)',
        brand: {
          50:  '#fdf4f9', 100: '#fce8f3', 200: '#fad1e8',
          300: '#f6aad3', 400: '#ef75b3', 500: '#e44e96',
          600: '#d02f79', 700: '#b0205f', 800: '#921d4f', 900: '#7a1d45',
        },
      },
      fontFamily: {
        sans:    ['"DM Sans"', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        display: ['"Outfit"', 'sans-serif'],
      },
      borderRadius: {
        'candy-card': '24px',
        'candy-pill': '9999px',
      },
      keyframes: {
        'bounce-subtle': {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%':      { transform: 'translateY(-4px)' },
        },
      },
      animation: {
        'bounce-subtle': 'bounce-subtle 2s infinite ease-in-out',
        'spin-slow':     'spin 8s linear infinite',
      },
    },
  },
  plugins: [],
}
