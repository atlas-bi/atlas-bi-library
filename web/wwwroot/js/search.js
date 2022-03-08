(function () {
  document
    .querySelector('.user-search')
    .addEventListener('keydown', function (e) {
      if (
        e.target.closest('input.dd-vsbl') &&
        !e.target.closest('.dd-wrp-show') &&
        (e.keyCode == 13 || e.keyCode == 3)
      ) {
        e.target.closest('form').submit();
      }
    });
});

// report search
(function () {
  var start, lastTitle;
  var d = document,
    w = window,
    grp = d.getElementById('search-form'),
    m = d.getElementsByClassName('body-mainCtn')[0];
  //hst = d.getElementsByClassName('sr-hst')[0];

  if (grp == undefined) {
    // no search on the current page
    return !1;
  }

  var i = grp.getElementsByTagName('input')[0],
    sAjx = null,
    atmr,
    a = document.createElement('a');

  /**
   * scroll to top when clicking search button
   */
  document.addEventListener('click', function (e) {
    if (e.target.closest('#nav-search')) {
      document.documentElement.scrollTop = document.body.scrollTop = 0;
    }
  });

  var oldHref = w.location.pathname.toLowerCase().startsWith('/search')
    ? '/'
    : w.location.href;
  lastTitle = document.title;

  w.oldPopState = document.location.pathname;

  function close(url) {
    a.href = url || oldHref;

    i.value = '';
    d.title = lastTitle;
    history.pushState(
      {
        state: 'ajax',
      },
      lastTitle,
      oldHref,
    );
  }

  function replaceUrlParam(url, paramName, paramValue) {
    if (paramValue == null) {
      paramValue = '';
    }
    var pattern = new RegExp('\\b(' + paramName + '=).*?(&|#|$)');
    if (url.search(pattern) >= 0) {
      if (paramValue == '') {
        return url.replace(pattern, '');
      }
      return url.replace(pattern, '$1' + paramValue + '$2');
    }
    url = url.replace(/[?#]$/, '');
    if (paramValue == '') {
      return url;
    }
    return (
      url + (url.indexOf('?') > 0 ? '&' : '?') + paramName + '=' + paramValue
    );
  }

  function getQueryStringParams(params, url) {
    // first decode URL to get readable data
    var href = decodeURIComponent(url || window.location.href);
    // regular expression to get value
    var regEx = new RegExp('[?&]' + params + '=([^&#]*)', 'i');
    var value = regEx.exec(href);
    // return the value if exist
    return value ? value[1] : null;
  }

  function AjaxSearch(value, url) {
    // show loader here
    if (document.querySelector('.body-mainCtn')) {
      document.querySelector('.body-mainCtn').style.opacity = 0.5;
    }

    // attempt to get existing search params from url
    var url_Path = window.location.pathname.toLowerCase() == '/search';

    var s = url;

    // if we are on the search page already, keep filters.
    if (url_Path) {
      value = value || getQueryStringParams('Query', url);
      s =
        url ||
        replaceUrlParam(
          replaceUrlParam(
            window.location.href.replace(window.location.origin, ''),
            'Query',
            value,
          ),
          'PageIndex',
          '',
        );
    } else {
      if (url) {
        s = url;
      } else {
        s = '/Search?Query=' + value;

        // add default filters for type depending on url
        if (window.location.pathname.toLowerCase() == '/users') {
          s += '&type=users';
        } else if (window.location.pathname.toLowerCase() == '/groups') {
          s += '&type=groups';
        }
      }
    }

    var u = s.replace('/Search?Query=', '');

    start = new Date();
    if (
      (typeof value !== 'undefined' && value !== null && value.length > 0) ||
      typeof url !== 'undefined'
    ) {
      document.documentElement.scrollTop = document.body.scrollTop = 0;
      //hst.style.display = 'none';

      if (typeof atmr !== 'undefined') clearTimeout(atmr);

      a.href = url || oldHref;

      w.oldPopState = document.location.pathname;
      history.pushState(
        {
          state: 'ajax',
          search: value,
        },
        document.title,
        w.location.origin + '/Search?Query=' + encodeURI(decodeURI(u)),
      );

      if (cache.exists(s)) {
        l(cache.get(s), a, m, d, atmr, s, u, value);
      } else {
        if (sAjx !== null) {
          sAjx.abort();
        }

        sAjx = new XMLHttpRequest();
        sAjx.open('get', s, true);
        sAjx.setRequestHeader(
          'Content-Type',
          'application/x-www-form-urlencoded; charset=UTF-8',
        );
        sAjx.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
        sAjx.send();

        sAjx.onload = function () {
          l(sAjx.responseText, a, m, d, atmr, s, u, value);
          var ccHeader =
            sAjx.getResponseHeader('Cache-Control') != null
              ? (sAjx.getResponseHeader('Cache-Control').match(/\d+/) || [
                  null,
                ])[0]
              : null;

          if (ccHeader) {
            cache.set(s, sAjx.responseText, ccHeader);
          }
        };
      }
    } else {
      d.dispatchEvent(
        new CustomEvent('load-ajax', {
          detail: oldHref,
        }),
      );
    }
  }

  var l = function l(t, a, m, d, atmr, s, u, value) {
    //hst.style.display = 'none';
    // remove nav links
    if (document.querySelector('.side-links')) {
      document.querySelector('.side-links').innerHTML = '';
    }

    // clear ads
    if (document.getElementById('AdColTwo')) {
      document.getElementById('AdColTwo').innerHTML = '';
    }

    m.style.visibility = 'visible';
    m.style.removeProperty('overflow');
    m.innerHTML = t;
    var sc = Array.prototype.slice.call(
      m.querySelectorAll('script:not([type="application/json"])'),
    );

    for (var x = 0; x < sc.length; x++) {
      var q = document.createElement('script');
      q.innerHTML = sc[x].innerHTML;
      q.type = 'text/javascript';
      q.setAttribute('async', 'true');
      m.appendChild(q);
      sc[x].parentElement.removeChild(sc[x]);
    }

    d.title = 'Search: ' + value + ' | Atlas BI Library';

    history.replaceState(
      {
        state: 'ajax',
        search: value,
      },
      document.title,
      w.location.origin + '/Search?Query=' + encodeURI(decodeURI(u)),
    );

    atmr = setTimeout(function () {
      document.dispatchEvent(
        new CustomEvent('analytics-post', {
          cancelable: true,
          detail: {
            value: new Date().getTime() - start.getTime(),
            type: 'newpage',
          },
        }),
      );
    }, 3000);

    document.dispatchEvent(new CustomEvent('related-reports'));
    document.dispatchEvent(new CustomEvent('ajax'));
    document.dispatchEvent(new CustomEvent('ss-load'));
    document.dispatchEvent(new CustomEvent('code-highlight'));

    /* remove loader here */
    if (document.querySelector('.body-mainCtn')) {
      document.querySelector('.body-mainCtn').style.opacity = '';
    }
  };

  grp.addEventListener('click', function (e) {
    e.stopPropagation();
  });
  grp.addEventListener('submit', function (e) {
    e.preventDefault();
  });

  // only search if the user has stopped typing for 1/5 second.
  var searchTimeout = 250,
    searchTimerId = null;
  i.addEventListener('input', function () {
    window.clearTimeout(searchTimerId);
    searchTimerId = window.setTimeout(function () {
      if (i.value.trim() !== '') {
        AjaxSearch(i.value, null);
        window.clearTimeout(searchTimerId);
      }
    }, searchTimeout);
  });

  d.addEventListener('click', function (e) {
    //hst.style.display = 'none';

    if (e.target.matches('.search-filter input')) {
      e.preventDefault();
      submit(e.target.closest('.search-filter input').value);
      return !1;
    } else if (e.target.matches('.page-link')) {
      if (e.target.closest('.search-filter input')) {
        submit(e.target.closest('.search-filter input').value);
      }
      return !1;
    }
  });

  function submit(l) {
    AjaxSearch(null, l);
  }

  w.onpopstate = function () {
    if (document.location.pathname == '/Search' || w.oldPopState == '/Search') {
      w.location.href = document.location.href;
    }
  };
})();
