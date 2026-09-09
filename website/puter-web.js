/**
 * Puter.js Integration Helper for Opencode / Ocean-ac Website
 * Official Puter.js SDK: https://js.puter.com/v2/puter.js
 * 
 * Features supported:
 * - puter.ai.chat(prompt, options) - Use free AI models (GPT-4o mini, Claude, Llama, etc. via Puter AI)
 * - puter.ai.complete(prompt, options) - Text completion
 * - puter.auth.* - User authentication & session management
 * - puter.kv.* - Key-Value store storage
 * - puter.fs.* - File system storage in Puter cloud
 */

(function(root, factory) {
  if (typeof define === 'function' && define.amd) {
    define([], factory);
  } else if (typeof module === 'object' && module.exports) {
    module.exports = factory();
  } else {
    root.PuterHelper = factory();
  }
}(typeof self !== 'undefined' ? self : this, function() {
  'use strict';

  const SCRIPT_URL = 'https://js.puter.com/v2/puter.js';

  let loaded = false;
  let loadPromise = null;

  function loadPuter() {
    if (loaded && typeof puter !== 'undefined') {
      return Promise.resolve(puter);
    }
    if (loadPromise) {
      return loadPromise;
    }

    loadPromise = new Promise((resolve, reject) => {
      if (typeof window === 'undefined') {
        return reject(new Error('Puter.js is designed for browser environment or requires puter package in Node.js.'));
      }
      if (typeof puter !== 'undefined') {
        loaded = true;
        return resolve(puter);
      }

      const script = document.createElement('script');
      script.src = SCRIPT_URL;
      script.async = true;
      script.onload = () => {
        loaded = true;
        resolve(window.puter);
      };
      script.onerror = (err) => {
        reject(new Error('Failed to load Puter.js script: ' + err));
      };
      document.head.appendChild(script);
    });

    return loadPromise;
  }

  return {
    load: loadPuter,
    
    // AI Chat helper (uses Puter AI free models)
    async chat(promptOrMessages, options = {}) {
      const p = await loadPuter();
      return await p.ai.chat(promptOrMessages, options);
    },

    // AI Completion helper
    async complete(prompt, options = {}) {
      const p = await loadPuter();
      return await p.ai.complete(prompt, options);
    },

    // Authentication helpers
    async signIn() {
      const p = await loadPuter();
      return await p.auth.signIn();
    },

    async signOut() {
      const p = await loadPuter();
      return await p.auth.signOut();
    },

    async getUser() {
      const p = await loadPuter();
      return await p.auth.getUser();
    },

    async isSignedIn() {
      const p = await loadPuter();
      return p.auth.isSignedIn();
    },

    // Key-Value store helpers
    async kvSet(key, value) {
      const p = await loadPuter();
      return await p.kv.set(key, value);
    },

    async kvGet(key) {
      const p = await loadPuter();
      return await p.kv.get(key);
    }
  };
}));
