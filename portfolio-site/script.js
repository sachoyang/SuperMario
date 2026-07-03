// Q&A accordion
document.querySelectorAll('.qa-q').forEach(q => {
  q.addEventListener('click', () => {
    q.parentElement.classList.toggle('open');
  });
});

// Scrollspy for sidebar nav
const sections = document.querySelectorAll('section[id]');
const navLinks = document.querySelectorAll('.nav-link');

function onScroll() {
  let currentId = '';
  const scrollPos = window.scrollY + 140;

  sections.forEach(sec => {
    if (scrollPos >= sec.offsetTop) {
      currentId = sec.id;
    }
  });

  navLinks.forEach(link => {
    link.classList.toggle('active', link.getAttribute('href') === '#' + currentId);
  });

  const topBtn = document.querySelector('.top-btn');
  if (topBtn) topBtn.classList.toggle('show', window.scrollY > 600);
}

window.addEventListener('scroll', onScroll);
onScroll();

document.querySelector('.top-btn')?.addEventListener('click', () => {
  window.scrollTo({ top: 0, behavior: 'smooth' });
});
