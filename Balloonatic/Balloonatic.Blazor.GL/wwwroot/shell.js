// window.__GAME — the Playwright-facing test bridge for the WebGL head.
//
// Every call is a thin promise wrapper over one .NET entry point (Bridge.BridgeCall), which forwards
// to Balloonatic.Core.TestHarness — the same object the xunit suite drives directly. Behaviour is
// asserted through this surface; screenshots are only ever for judging how it looks.
//
//   await window.__GAME.ready            // true once a real frame has been drawn
//   window.__GAME.readyAtMs              // cold boot: page start -> first frame, measured in-page
//   window.__GAME.version()              // bridge protocol version
//   window.__GAME.screen()               // "scaffold" today, "title"/"level" later
//   window.__GAME.state()                // JSON snapshot (parsed)
//   window.__GAME.reset('1234')          // new seed, clears input, paused = true
//   window.__GAME.set('heat', 1)         // inject input: heat (hold) and ballast (one drop)
//   window.__GAME.step(60)               // advance 60 fixed steps, returns the state hash
//   window.__GAME.hash() / tick() / seed()
//   window.__GAME.pause() / resume()     // hand the clock to/from the test
//   window.__GAME.frames()               // renderer frame counter (for frame-rate budgets)
(function () {
    const assembly = 'Balloonatic';
    const startedAt = performance.now();

    async function raw(name, arg) {
        try {
            if (!window.DotNet) {
                return 'error:no-dotnet-runtime';
            }
            return await window.DotNet.invokeMethodAsync(assembly, 'BridgeCall', name,
                arg === undefined || arg === null ? '' : String(arg));
        } catch (failure) {
            return 'error:' + (failure && failure.message ? failure.message : failure);
        }
    }

    async function text(name, arg) {
        return await raw(name, arg);
    }

    const api = {
        ready: false,
        readyAtMs: 0,
        bootError: '',
        raw: raw,
        version: function () { return text('version'); },
        screen: function (id) { return text('screen', id); },
        seed: function () { return text('seed'); },
        tick: function () { return text('tick'); },
        temperature: function () { return text('temperature'); },
        altitude: function () { return text('altitude'); },
        velocity: function () { return text('velocity'); },
        x: function () { return text('x'); },
        velocityX: function () { return text('velocityX'); },
        level: function (id) { return text('level', id); },
        ballast: function () { return text('ballast'); },
        hash: function () { return text('hash'); },
        frames: function () { return text('frames'); },
        metrics: function () { return text('metrics'); },
        reset: function (seed) { return text('reset', seed); },
        set: function (key, value) { return text('set', key + '=' + (value === 0 || value === false ? 0 : 1)); },
        step: function (n) { return text('step', n); },
        pause: function () { return text('pause'); },
        resume: function () { return text('resume'); },
        state: async function () {
            const json = await text('state');
            try {
                return JSON.parse(json);
            } catch (failure) {
                return { error: json };
            }
        }
    };

    // The canvas is sized by the template's initRenderJS, but the .NET device does not learn that
    // size on its own, so the entry rendered into a small corner of the page. The shell owns the
    // pixel budget: it reports the holder's CSS size and the head matches its drawing surface to it.
    // Letterboxing stays a render-time decision in DesignCanvas, so an LD iframe of any shape works.
    async function syncSurface() {
        const holder = document.getElementById('canvasHolder');
        const canvas = document.getElementById('theCanvas');
        if (!holder || !canvas) {
            return '';
        }
        const width = Math.max(1, Math.floor(holder.clientWidth));
        const height = Math.max(1, Math.floor(holder.clientHeight));
        if (canvas.width !== width || canvas.height !== height) {
            canvas.width = width;
            canvas.height = height;
        }
        return await raw('surface', width + 'x' + height);
    }

    let resizeTimer = 0;
    window.addEventListener('resize', function () {
        if (!api.ready) {
            return;
        }
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(syncSurface, 100);
    });

    // The WASM runtime, the .NET assemblies and the first drawn frame all arrive on their own
    // schedule, and that schedule is machine-dependent — so readiness is polled, never assumed, and
    // no test ever asserts on an absolute boot time (only records it).
    let attempts = 0;
    async function pollUntilReady() {
        const reply = await raw('ready');
        if (reply === '1') {
            api.ready = true;
            api.readyAtMs = Math.round(performance.now() - startedAt);
            await syncSurface();
            return;
        }
        if (attempts++ < 1200) {
            setTimeout(pollUntilReady, 250);
        } else {
            api.bootError = reply;
        }
    }
    pollUntilReady();
    setTimeout(function () {
        if (!api.ready) {
            api.bootError = 'timed out waiting for the first drawn frame';
        }
    }, 180_000);

    window.__GAME = api;
})();
