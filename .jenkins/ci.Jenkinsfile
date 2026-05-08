pipeline {
    agent any

    environment {
        // These are typically already available in Jenkins, but we make it explicit
        COMMIT_SHA = "${env.GIT_COMMIT}"
        BUILD_ID   = "${env.GIT_BRANCH}-${env.BUILD_NUMBER}"
        BUILD_URL  = "${env.BUILD_URL}"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Write build metadata') {
            steps {
                writeFile file: 'build.json', text: """
                {
                  "commit": "${env.GIT_COMMIT}",
                  "imageTag": "${env.IMAGE_TAG}",
                  "branch": "${env.GIT_BRANCH}",
                  "buildNumber": "${env.BUILD_NUMBER}"
                }
                """
                archiveArtifacts artifacts: 'build.json'
            }
        }

        stage('Set image tag') {
            steps {
                script {
                    env.IMAGE_TAG = env.GIT_COMMIT
                    echo "Using IMAGE_TAG=${env.IMAGE_TAG}"
                }
            }
        }

        stage('Build Docker images') {
            steps {
                script {
                    withEnv(["IMAGE_TAG=${env.IMAGE_TAG}"]) {
                        sh """
                            docker compose -f compose.build.yml \
                            --profile build-app build \
                            --build-arg COMMIT_SHA=${COMMIT_SHA} \
                            --build-arg BUILD_ID=${BUILD_ID} \
                            --build-arg BUILD_URL=${BUILD_URL}
                        """
                    }
                }
            }
        }

        stage('Push Docker Images') {
            steps {
                sh """
                    docker compose -f compose.build.yml push web api migrator
                """
            }
        }
    }

    post {
        success {
            echo 'Docker images built successfully.'
        }
        failure {
            echo 'Build failed.'
        }
    }
}
