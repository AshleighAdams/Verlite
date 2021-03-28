const template = GitgraphJS.templateExtend(GitgraphJS.TemplateName.Metro, {
    commit: {
        message: {
            displayHash: false,
            displayAuthor: false,
            displayBranch: true,
        }
    }
})

const graphContainer = document.getElementById("graph-container");
const gitgraph = GitgraphJS.createGitgraph(graphContainer, {
  template: template
});

const master = gitgraph.branch("master")
  .commit("Versioned as: 0.1.0")
  .tag("v0.1.0");

const featureC = master.branch("old-feature")
  .commit("Versioned as: 0.1.1-alpha.1")

master.commit("Versioned as: 1.0.0 (before tag: 0.1.1-alpha.1)")
  .tag("v1.0.0");

const featureA = master.branch("some-fix")
  .commit("Versioned as: 1.0.1-alpha.1")
  .commit("Versioned as: 1.0.1-alpha.2")
  .commit("Versioned as: 1.0.1-alpha.3");

featureC.merge(master, "Versioned as: 0.1.1-alpha.2");

const featureB = master.branch("new-fix")
  .commit("Versioned as: 1.0.1-alpha.1");

master
  .merge(featureC, "Versioned as: 1.0.1-alpha.1")
  .merge(featureA, "Versioned as: 1.0.1-alpha.2")
  .merge(featureB, "Versioned as: 1.0.1-rc.1 (before tag: 1.0.1-alpha.3)")
  .tag("v1.0.1-rc.1")
  .commit("Versioned as: 1.0.1-rc.1.1")
  .commit("Versioned as: 1.0.1-rc.1.2")
  .commit("Versioned as: 1.0.1 (before tag: 1.0.1-rc.1.3, then 1.0.1-rc.2)")
  .tag("v1.0.1-rc.2")
  .tag("v1.0.1");

