document.addEventListener('DOMContentLoaded', () => {
  const codeBlocks = document.querySelectorAll('pre');

  for (const block of codeBlocks) {
    const code = block.querySelector('code');
    if (!code) {
      continue;
    }

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'copy-button';
    button.textContent = 'Copy';
    button.setAttribute('aria-label', 'Copy code to clipboard');

    button.addEventListener('click', async () => {
      try {
        await navigator.clipboard.writeText(code.innerText.trimEnd());
        button.textContent = 'Copied';
        button.classList.add('is-copied');

        window.setTimeout(() => {
          button.textContent = 'Copy';
          button.classList.remove('is-copied');
        }, 1500);
      } catch {
        button.textContent = 'Failed';

        window.setTimeout(() => {
          button.textContent = 'Copy';
        }, 1500);
      }
    });

    block.appendChild(button);
  }

  const tocLinks = [...document.querySelectorAll('.page-toc-nav a[href^="#"]')];
  const sectionTargets = tocLinks
    .map((link) => {
      const href = link.getAttribute('href');
      if (!href) {
        return null;
      }

      const section = document.querySelector(href);
      return section ? { link, section } : null;
    })
    .filter(Boolean);

  if (sectionTargets.length === 0) {
    return;
  }

  const updateActiveSection = () => {
    const scrollPosition = window.scrollY + 140;
    let active = sectionTargets[0];

    for (const entry of sectionTargets) {
      if (entry.section.offsetTop <= scrollPosition) {
        active = entry;
      }
    }

    for (const entry of sectionTargets) {
      entry.link.classList.toggle('active', entry === active);
    }
  };

  updateActiveSection();
  window.addEventListener('scroll', updateActiveSection, { passive: true });
  window.addEventListener('hashchange', updateActiveSection);
});
