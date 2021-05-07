pipeline {
  agent none
  stages {
    stage('Initialize') {
      agent {
        node {
          label "${env.AGENT_PREFIX}"
        }

      }
      steps {
        echo "Initialized on node: ${env.NODE_NAME}"

        checkout scm
        script {
            def datas = readYaml file: "${env.CONFIG_FILE}"

            env.GOOGLE_DOCKER_REGISTRY = datas.google.docker.registry

            env.MAPPING_PROD_BRANCH = datas.mapping.prod.branch
            env.MAPPING_PROD_PRERELEASE = datas.mapping.prod.prerelease
            env.MAPPING_TEST_BRANCH = datas.mapping.test.branch
            env.MAPPING_TEST_PRERELEASE = datas.mapping.test.prerelease
            env.MAPPING_DEV_BRANCH = datas.mapping.dev.branch
            env.MAPPING_DEV_PRERELEASE = datas.mapping.dev.prerelease

            def mapping = ""
            if(env.BRANCH_NAME == env.MAPPING_PROD_BRANCH) {
                mapping = datas.mapping.prod
            }
            else if(env.BRANCH_NAME == env.MAPPING_TEST_BRANCH) {
                mapping = datas.mapping.test
            }
            else if(env.BRANCH_NAME == env.MAPPING_DEV_BRANCH) {
                mapping = datas.mapping.dev
            }

            if(mapping != "") {
                env.ANGULAR_ENV = mapping.angular.environment

                env.SERVICE_NAME = mapping.cloudrun.service_name
                env.REGION = mapping.cloudrun.region
                env.IMAGE_NAME = mapping.cloudrun.image_name
                env.ENVIRONMENT = mapping.cloudrun.env
                env.PORT = mapping.cloudrun.port
                env.SERVICE_ACCOUNT = mapping.cloudrun.service_account
                env.MEMORY = mapping.cloudrun.memory
                env.CPU = mapping.cloudrun.cpu
                env.TIMEOUT = mapping.cloudrun.timeout
                env.MAXIMUM_REQUESTS = mapping.cloudrun.maximum_requests
                env.MAX_INSTANCES = mapping.cloudrun.max_instances
                env.DB_INSTANCE = mapping.cloudrun.db_instance
                env.VPC_CONNECTOR = mapping.cloudrun.vpc_connector
                env.VPC_EGRESS = mapping.cloudrun.vpc_egress
            }
        }

    }
  }

  stage('Test') {
    agent {
      node {
        label "${env.AGENT_PREFIX}"
      }

    }
    tools {
        nodejs "nodejs"
    }
    steps {
        dir("API") {
            sh (
                script: 'dotnet test -c Release -v normal --collect:"XPlat Code Coverage" --settings coverlet.runsettings --no-build',
                label: 'Dotnet Test'
            )
        }
        dir("client") {
            sh (
                script: 'npm run test -- --no-watch --no-progress --code-coverage --browsers=ChromeHeadless',
                label: 'Angular/NodeJS Test'
            )
        }
    }
  }

  stage('Preparing for build') {
    agent {
      node {
        label "${env.AGENT_PREFIX}"
      }

    }
    when {
      beforeAgent true
      anyOf {
        branch env.MAPPING_PROD_BRANCH;
        branch env.MAPPING_TEST_BRANCH;
        branch env.MAPPING_DEV_BRANCH
      }

    }
    steps {
        script {
            semantic.init env.MAPPING_PROD_BRANCH, env.MAPPING_TEST_BRANCH, env.MAPPING_DEV_BRANCH, env.MAPPING_PROD_PRERELEASE, env.MAPPING_TEST_PRERELEASE, env.MAPPING_DEV_PRERELEASE
        }

        script {
            env.VERSION = semantic.version "${env.GITHUB_CREDENTIALS_ID}"
        }

        echo "VERSION=${env.VERSION}"

        script {
            cloud.login env.GOOGLE_PROJECT, env.JENKINS_CREDENTIALS_ID
        }
        
        script {
            cloud.configureDocker
        }

    }
  }

  stage('Build') {
    when {
      beforeAgent true
      anyOf {
        branch env.MAPPING_PROD_BRANCH;
        branch env.MAPPING_TEST_BRANCH;
        branch env.MAPPING_DEV_BRANCH
      }

    }
    steps {
      sh (
        script: "docker build --build-arg environment=${env.ANGULAR_ENV} -t ${env.GOOGLE_DOCKER_REGISTRY}:${env.VERSION} .",
        label: "Docker build for ${env.GOOGLE_DOCKER_REGISTRY} with tag ${env.VERSION}"
      )
      sh (
        script: "docker build --build-arg environment=${env.ANGULAR_ENV} -t ${env.GOOGLE_DOCKER_REGISTRY}:latest .",
        label: "Docker build ${env.GOOGLE_DOCKER_REGISTRY} with tag latest"
      )

    }
  }

  stage('Publish') {
    agent {
      node {
        label "${env.AGENT_PREFIX}"
      }

    }
    when {
      beforeAgent true
      anyOf {
        branch env.MAPPING_PROD_BRANCH;
        branch env.MAPPING_TEST_BRANCH;
        branch env.MAPPING_DEV_BRANCH
      }

    }
    steps {
        script {
            semantic.release "${env.GITHUB_CREDENTIALS_ID}"
        }

    }
  }

  stage('Deploy') {
    agent {
      node {
        label "${env.AGENT_PREFIX}"
      }

    }
    when {
      beforeAgent true
      anyOf {
        branch env.MAPPING_PROD_BRANCH;
        branch env.MAPPING_TEST_BRANCH;
        branch env.MAPPING_DEV_BRANCH
      }

    }
    steps {
        script {
            try {
                sh (
                    script: "docker push ${env.GOOGLE_DOCKER_REGISTRY}:${env.VERSION}",
                    label: "Docker push image ${env.GOOGLE_DOCKER_REGISTRY}:${env.VERSION} to Google registry"
                )

                cloud.deployToRun env.SERVICE_NAME env.REGION env.GOOGLE_DOCKER_REGISTRY env.VERSION env.ENVIRONMENT env.PORT env.SERVICE_ACCOUNT env.MEMORY env.CPU env.TIMEOUT env.MAXIMUM_REQUESTS env.MAX_INSTANCES env.DB_INSTANCE env.VPC_CONNECTOR env.VPC_EGRESS
            }
            catch(err) {
                semantic.rollback "${env.GITHUB_CREDENTIALS_ID}"
                currentBuild.result = 'FAILURE'
                throw err
            }
        }

        sh (
            script: "docker push ${env.GOOGLE_DOCKER_REGISTRY}:latest",
            label: "Docker push image ${env.GOOGLE_DOCKER_REGISTRY}:latest to Google registry"
        )
    }
  }

}
environment {
  GOOGLE_PROJECT = 'unity-firebuild'
  AGENT_PREFIX = 'jenkins-agent'
  JENKINS_CREDENTIALS_ID = 'jenkins-sa'
  GITHUB_CREDENTIALS_ID = 'github-credentials'
  CONFIG_FILE = 'aspnetcore.yml'
  EMAIL_ADDRESS = 'development@naughtybikergames.io'
}
post {
  success {
    emailext(to: "${env.EMAIL_ADDRESS}", subject: "${env.JOB_NAME} - Build #${env.BUILD_NUMBER} - SUCCESS!", body: """
                                                                <html>
                                                                  <header></header>
                                                                  <body>
                                                                    <img src="${env.JENKINS_URL}/static/c5c835c9/images/48x48/blue.png" alt="blue" width="48" height="48" style="float:left" />
                                                                    <h1>BUILD SUCCESS</h1>
                                                                    <p>Project: UnityCI<br>
                                                                    Job: ${env.JOB_NAME}<br>
                                                                    Date of build: ${env.BUILD_TIMESTAMP}<br>
                                                                    Build duration: ${currentBuild.durationString}<br><br>
                                                                    Check console output <a href="${env.BUILD_URL}">here</a> to view the results.</p>
                                                                  </body>
                                                                </html>
                                                                                  """)
  }

  failure {
    emailext(to: "${env.EMAIL_ADDRESS}", subject: "${env.JOB_NAME} - Build #${env.BUILD_NUMBER} - FAILED!", body: """
                                                                <html>
                                                                  <header></header>
                                                                  <body>
                                                                    <img src="${env.JENKINS_URL}/static/c5c835c9/images/48x48/red.png" alt="red" width="48" height="48" style="float:left" />
                                                                    <h1>BUILD FAILED</h1>
                                                                    <p>Project: UnityCI<br>
                                                                    Job: ${env.JOB_NAME}<br>
                                                                    Date of build: ${env.BUILD_TIMESTAMP}<br>
                                                                    Build duration: ${currentBuild.durationString}<br><br>
                                                                    Check console output <a href="${env.BUILD_URL}">here</a> to view the results.</p>
                                                                  </body>
                                                                </html>
                                                                                  """)
  }

  aborted {
    emailext(to: "${env.EMAIL_ADDRESS}", subject: "${env.JOB_NAME} - Build #${env.BUILD_NUMBER} - ABORTED!", body: """
                                                                <html>
                                                                  <header></header>
                                                                  <body>
                                                                    <img src="${env.JENKINS_URL}/static/c5c835c9/images/48x48/aborted.png" alt="aborted" width="48" height="48" style="float:left" />
                                                                    <h1>BUILD ABORTED</h1>
                                                                    <p>Project: UnityCI<br>
                                                                    Job: ${env.JOB_NAME}<br>
                                                                    Date of build: ${env.BUILD_TIMESTAMP}<br>
                                                                    Build duration: ${currentBuild.durationString}<br><br>
                                                                    Check console output <a href="${env.BUILD_URL}">here</a> to view the results.</p>
                                                                  </body>
                                                                </html>
                                                                                  """)
  }

  always {
    node(env.AGENT_PREFIX) {
      sh(
        script: """
            docker rm -vf \$(docker ps -a -q) || echo 0
            docker rmi -f \$(docker images -a -q) || echo 0
        """,
        label: 'Post docker cleanup'
      )
    }

  }

}
options {
  skipDefaultCheckout(true)
  disableConcurrentBuilds()
}
}
