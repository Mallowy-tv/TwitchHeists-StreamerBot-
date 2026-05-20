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
      }
      catch {
        button.textContent = 'Failed';

        window.setTimeout(() => {
          button.textContent = 'Copy';
        }, 1500);
      }
    });

    block.appendChild(button);
  }
});
