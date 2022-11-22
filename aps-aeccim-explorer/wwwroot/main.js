﻿import { initViewer, loadModel } from './viewer.js'
var globalViewer = '';

window.addEventListener("load", async () => {
  const login = document.getElementById('login');
  const urnInput = document.getElementById('modelurn');
  const projectidInput = document.getElementById('projectid');
  const viewerToggle = document.getElementById('toggleviewer');
  const viewerDiv = document.getElementById('viewer');
  const graphiqlDiv = document.getElementById('graphiql');
  new ResizeObserver(() => {
    viewerDiv.style.height = `calc( ${document.body.scrollHeight}px - (1em + ${graphiqlDiv.clientHeight}px))`;
    if(!!globalViewer)
      globalViewer.resize();
  }).observe(graphiqlDiv);
  initViewer(viewerDiv).then(viewer => {
    globalViewer = viewer;
  });
  viewerToggle.onclick = async (cb) => {
    try {
      if (cb.target.checked) {
        let versions = await (await fetch(`/api/hubs/${projectidInput.value}/contents/${urnInput.value}/versions`)).json();
        await resizeGraphiql(graphiqlDiv, false);
        await loadNDisplayModel(graphiqlDiv, viewerDiv, globalViewer, versions[0].id);
      }
      else {
        hideModel(viewerDiv);
        await resizeGraphiql(graphiqlDiv, true);
      }
    } catch (error) {
      console.error(error);
      cb.target.checked = false;
    }
  }
  try {
    const resp = await fetch('/api/auth/profile');
    if (resp.ok) {
      const user = await resp.json();
      login.innerText = `Logout (${user.name})`;
      login.onclick = () => window.location.replace('/api/auth/logout');
    } else {
      login.innerText = 'Login';
      login.onclick = () => window.location.replace('/api/auth/login');
    }
    login.style.visibility = 'visible';
  } catch (err) {
    alert('Could not initialize the application. See console for more details.');
    console.error(err);
  }
})

async function resizeGraphiql(graphiqlDiv ,increase) {
  if (increase) {
    graphiqlDiv.style.height = 'calc(100% - 3em)';
  }
  else {
    graphiqlDiv.style.height = 'calc(70%)';
  }
}

async function loadNDisplayModel(graphiqlDiv, viewerDiv, viewer, urn) {
  try {
    viewerDiv.style.visibility = 'visible';
    viewerDiv.style.height = `calc( ${document.body.scrollHeight}px - (1em + ${graphiqlDiv.clientHeight}px))`;
    viewer.resize();
    loadModel(viewer, btoa(urn)).then();
  }
  catch (err) {
    console.log(`Not able to load the model: ${err}`);
  }
}

async function hideModel(viewerDiv) {
  try {
    viewerDiv.style.visibility = 'hidden';
  }
  catch (err) {
    console.log(`Not able to load the model: ${err}`);
  }
}