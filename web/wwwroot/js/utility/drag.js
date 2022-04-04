(function () {
  var d = document,
    d1 = 0,
    d2 = 0,
    dragEl,
    dragSEl;
  d.addEventListener(
    'mousedown',
    function (e) {
      if (e.target.closest('.drg-hdl')) {
        dragSEl = e.target.closest('.drg');
      } else {
        return;
      }

      dragEl = dragSEl.cloneNode(true);
      dragSEl.classList.add('drag-source');
      dragEl.classList.add('drag');
      dragEl.style.width = dragSEl.offsetWidth + 1 + 'px';

      // margin is not included in x and y.
      var style = getComputedStyle(dragSEl);

      d1 = e.clientY - getOffset(dragSEl).top + parseInt(style.marginTop, 10);
      d2 = e.clientX - getOffset(dragSEl).left + parseInt(style.marginLeft, 10);
      dragSEl.parentElement.style.position = 'relative';
      dragSEl.parentElement.appendChild(dragEl);
      dragEl.style.top = e.clientY - d1 + 'px';
      dragEl.style.left = e.clientX - d2 + 'px';

      dragMouseDown(e);
    },
    false,
  );

  var dragMouseDown = function dragMouseDown(e) {
      e = e || window.event;
      e.preventDefault();
      d.addEventListener('mouseup', dragMouseUp);
      d.addEventListener('mousemove', dragMouseMove);
    },
    dragMouseMove = function dragMouseMove(e) {
      e = e || window.event;
      e.preventDefault();
      dragEl.style.top = e.clientY - d1 + 'px';
      dragEl.style.left = e.clientX - d2 + 'px';
      dragEl.dispatchEvent(
        new CustomEvent('dragMove', {
          cancelable: true,
          bubbles: true,
          detail: {
            el: dragEl,
            x: e.clientX,
            y: e.clientY,
          },
        }),
      );
    },
    dragMouseUp = function dragMouseUp(e) {
      dragSEl.parentElement.replaceChild(dragEl, dragSEl);
      dragEl.classList.remove('drag');
      dragEl.dispatchEvent(
        new CustomEvent('dragEnd', {
          cancelable: true,
          bubbles: true,
          detail: {
            el: dragEl,
            x: e.clientX,
            y: e.clientY,
          },
        }),
      );

      dragEl.style.width = '';
      dragEl.style.top = '';
      dragEl.style.left = '';
      d.removeEventListener('mouseup', dragMouseUp);
      d.removeEventListener('mousemove', dragMouseMove);
    };
})();