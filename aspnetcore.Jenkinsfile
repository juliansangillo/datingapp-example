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

            env.GOOGLE_PROJECT = datas.google?.project
            env.GOOGLE_DOCKER_REGISTRY = datas.google?.docker?.registry

            env.MAPPING_PROD_BRANCH = datas.mapping?.prod?.branch
            env.MAPPING_PROD_PRERELEASE = datas.mapping?.prod?.prerelease
            env.MAPPING_TEST_BRANCH = datas.mapping?.test?.branch
            env.MAPPING_TEST_PRERELEASE = datas.mapping?.test?.prerelease
            env.MAPPING_DEV_BRANCH = datas.mapping?.dev?.branch
            env.MAPPING_DEV_PRERELEASE = datas.mapping?.dev?.prerelease

            def mapping = null
            if(env.BRANCH_NAME == env.MAPPING_PROD_BRANCH) {
                mapping = datas.mapping.prod
            }
            else if(env.BRANCH_NAME == env.MAPPING_TEST_BRANCH) {
                mapping = datas.mapping.test
            }
            else if(env.BRANCH_NAME == env.MAPPING_DEV_BRANCH) {
                mapping = datas.mapping.dev
            }

            env.IS_VALID = env.GOOGLE_PROJECT != "" && env.GOOGLE_DOCKER_REGISTRY != "" && mapping?.angular != null && mapping?.cloudrun != null

            if(env.IS_VALID == "true") {
                env.ANGULAR_ENV = mapping.angular.environment

                env.ENVIRONMENT = mapping.cloudrun.env
                env.PORT = mapping.cloudrun.port
                env.MEMORY = mapping.cloudrun.memory
                env.CPU = mapping.cloudrun.cpu
                env.TIMEOUT = mapping.cloudrun.timeout
                env.MAXIMUM_REQUESTS = mapping.cloudrun.maximum_requests
                env.MAX_INSTANCES = mapping.cloudrun.max_instances

                env.SERVICE_NAME = ""
                env.REGION = ""
                env.SERVICE_ACCOUNT = ""
                env.DB_INSTANCE = ""
                env.VPC_CONNECTOR = ""
                env.VPC_EGRESS = ""
                env.ALLOW_UNAUTHENTICATED = ""

                def services = mapping.cloudrun.services
                def delimiter = ";"
                def prefix = ""
                for(service in services) {
                    env.SERVICE_NAME += prefix + service.service_name
                    env.REGION += prefix + service.region
                    env.SERVICE_ACCOUNT += prefix + service.service_account
                    
                    env.DB_INSTANCE += prefix
                    if(service.db_instance)
                        env.DB_INSTANCE += service.db_instance
                    
                    env.VPC_CONNECTOR += prefix
                    if(service.vpc_connector)
                        env.VPC_CONNECTOR += service.vpc_connector

                    env.VPC_EGRESS += prefix
                    if(service.vpc_egress)
                        env.VPC_EGRESS += service.vpc_egress

                    env.ALLOW_UNAUTHENTICATED += prefix
                    if(service.allow_unauthenticated)
                        env.ALLOW_UNAUTHENTICATED += service.allow_unauthenticated
                    else
                        env.ALLOW_UNAUTHENTICATED += "false"

                    prefix = delimiter
                }
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
    steps {
        dir("API") {
            sh (
                script: 'dotnet test -c Release -v normal --collect:"XPlat Code Coverage" --settings coverlet.runsettings --no-build',
                label: 'Dotnet Test'
            )
        }
        dir("client") {
            sh (
                script: 'npm install',
                label: 'Install dependencies'
            )
            sh (
                script: 'ng test -- --no-watch --no-progress --code-coverage --browsers=ChromeHeadless',
                label: 'Angular Test'
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
            semantic.init env.MAPPING_PROD_BRANCH, env.MAPPING_TEST_BRANCH, env.MAPPING_DEV_BRANCH, env.MAPPING_PROD_PRERELEASE, env.MAPPING_TEST_PRERELEASE, env.MAPPING_DEV_PRERELEASE, 'aspnetcore'
        }

        script {
            env.VERSION = semantic.version "${env.GITHUB_CREDENTIALS_ID}"
        }

        echo "VERSION=${env.VERSION}"

        script {
            cloud.login env.GOOGLE_PROJECT, env.JENKINS_CREDENTIALS_ID
        }
        
        script {
            cloud.configureDocker()
        }

    }
  }

  stage('Build') {
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
      expression {
        env.IS_VALID == "true" && env.VERSION.trim()
      }

    }
    steps {
      sh (
        script: 'docker build --build-arg environment=$ANGULAR_ENV -t $GOOGLE_DOCKER_REGISTRY:$VERSION .',
        label: "Docker build for ${env.GOOGLE_DOCKER_REGISTRY} with tag ${env.VERSION}"
      )
      sh (
        script: 'docker build --build-arg environment=$ANGULAR_ENV -t $GOOGLE_DOCKER_REGISTRY:latest .',
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
      expression {
        env.IS_VALID == "true" && env.VERSION.trim()
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
      expression {
        env.IS_VALID == "true" && env.VERSION.trim()
      }

    }
    steps {
        script {
            try {
                sh (
                    script: 'docker push $GOOGLE_DOCKER_REGISTRY:$VERSION',
                    label: "Docker push image ${env.GOOGLE_DOCKER_REGISTRY}:${env.VERSION} to Google registry"
                )

                parallelize env.AGENT_PREFIX, env.SERVICE_NAME.split(';'), { service_name ->
                    def index = env.SERVICE_NAME.tokenize(';').indexOf(service_name)

                    def region = env.REGION.split(';')[index]
                    def service_account = env.SERVICE_ACCOUNT.split(';')[index]
                    def db_instance = env.DB_INSTANCE.split(';')[index]
                    def vpc_connector = env.VPC_CONNECTOR.split(';')[index]
                    def vpc_egress = env.VPC_EGRESS.split(';')[index]
                    def allow_unauthenticated = env.ALLOW_UNAUTHENTICATED.split(';')[index]

                    cloud.deployToRun service_name, region, env.GOOGLE_DOCKER_REGISTRY, env.VERSION, env.ENVIRONMENT, env.PORT, service_account, env.MEMORY, env.CPU, env.TIMEOUT, env.MAXIMUM_REQUESTS, env.MAX_INSTANCES, db_instance, vpc_connector, vpc_egress, allow_unauthenticated
                }
            }
            catch(err) {
                semantic.rollback "${env.GITHUB_CREDENTIALS_ID}"
                currentBuild.result = 'FAILURE'
                throw err
            }
        }

        sh (
            script: 'docker push $GOOGLE_DOCKER_REGISTRY:latest',
            label: "Docker push image ${env.GOOGLE_DOCKER_REGISTRY}:latest to Google registry"
        )
    }
  }

}
environment {
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
